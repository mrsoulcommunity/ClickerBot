using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ClickerBot;

/// <summary>
/// A tiny local HTTP server that lets a phone on the same network start and stop a run — the
/// site the app itself hosts, so there is nothing to install on the phone beyond a browser.
///
/// Binds to loopback plus every LAN IPv4 address individually rather than the usual wildcard
/// prefix (<c>http://+:PORT/</c>): the wildcard form needs either admin rights or a one-time
/// <c>netsh http add urlacl</c> reservation on Windows, while a concrete address does not,
/// which is what lets this run under the app's own normal-user manifest with nothing to set up.
/// </summary>
internal sealed class RemoteControlServer : IDisposable
{
    public const int Port = 8787;

    // The phone page's JS reads camelCase fields off the status response, which is the normal
    // convention for a JSON API and not what System.Text.Json emits by default for a PascalCase
    // C# record.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private HttpListener? _listener;
    private CancellationTokenSource? _cts;

    public bool IsRunning => _listener is { IsListening: true };

    public string Pin { get; private set; } = string.Empty;

    /// <summary>Every address the server is actually reachable at, loopback last.</summary>
    public IReadOnlyList<string> Urls { get; private set; } = Array.Empty<string>();

    /// <summary>
    /// Supplied by the owner. All three are invoked from a background thread — the HTTP
    /// request thread — so the owner is responsible for marshaling onto its own UI thread.
    /// </summary>
    public Func<RemoteStatusPayload>? GetStatus { get; set; }

    public Action? RequestStart { get; set; }

    public Action? RequestStop { get; set; }

    /// <returns>Null on success, or a message describing why the server could not start.</returns>
    public string? Start()
    {
        if (IsRunning)
        {
            return null;
        }

        Pin = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        var addresses = LocalIPv4Addresses().ToList();
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
        foreach (string address in addresses)
        {
            listener.Prefixes.Add($"http://{address}:{Port}/");
        }

        try
        {
            listener.Start();
        }
        catch (Exception ex) when (ex is HttpListenerException or SocketException)
        {
            listener.Close();
            return Loc.CouldNotStartRemoteServer(Port, ex.Message);
        }

        _listener = listener;
        _cts = new CancellationTokenSource();
        Urls = addresses.Select(a => $"http://{a}:{Port}/").Append($"http://127.0.0.1:{Port}/").ToList();

        _ = AcceptLoopAsync(listener, _cts.Token);
        return null;
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
        Urls = Array.Empty<string>();

        var listener = _listener;
        _listener = null;
        if (listener is null)
        {
            return;
        }

        try
        {
            listener.Stop();
            listener.Close();
        }
        catch (ObjectDisposedException)
        {
            // Already gone.
        }
    }

    public void Dispose() => Stop();

    private async Task AcceptLoopAsync(HttpListener listener, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Stop() closes the listener out from under a pending GetContextAsync, which
                // surfaces here as an exception rather than a clean cancellation — that is the
                // expected way this loop ends, not a fault worth reporting. Any other cause
                // leaves the listener equally unusable, so exiting is correct either way.
                break;
            }

            _ = HandleAsync(context);
        }
    }

    private async Task HandleAsync(HttpListenerContext context)
    {
        try
        {
            await RouteAsync(context).ConfigureAwait(false);
        }
        catch (Exception)
        {
            TrySetStatus(context.Response, 500);
        }
        finally
        {
            try
            {
                context.Response.Close();
            }
            catch (Exception)
            {
                // The phone likely disconnected already; nothing left to do.
            }
        }
    }

    private async Task RouteAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        string path = request.Url?.AbsolutePath ?? "/";

        if (request.HttpMethod == "GET" && path == "/")
        {
            await WriteAsync(response, "text/html; charset=utf-8", Encoding.UTF8.GetBytes(BuildPageHtml(Loc.Current)))
                .ConfigureAwait(false);
            return;
        }

        if (request.HttpMethod == "GET" && path == "/api/status")
        {
            var payload = GetStatus?.Invoke() ?? new RemoteStatusPayload(false, "", "", 0, null, 0, "Not ready");
            await WriteAsync(response, "application/json; charset=utf-8",
                    JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions))
                .ConfigureAwait(false);
            return;
        }

        if (request.HttpMethod == "POST" && path == "/api/verify")
        {
            response.StatusCode = HasValidPin(request) ? 200 : 401;
            return;
        }

        if (request.HttpMethod == "POST" && path == "/api/start")
        {
            if (!HasValidPin(request))
            {
                response.StatusCode = 401;
                return;
            }

            RequestStart?.Invoke();
            response.StatusCode = 200;
            return;
        }

        if (request.HttpMethod == "POST" && path == "/api/stop")
        {
            if (!HasValidPin(request))
            {
                response.StatusCode = 401;
                return;
            }

            RequestStop?.Invoke();
            response.StatusCode = 200;
            return;
        }

        response.StatusCode = 404;
    }

    private bool HasValidPin(HttpListenerRequest request)
    {
        string? supplied = request.Headers["X-Pin"];
        return !string.IsNullOrEmpty(supplied) && FixedTimeEquals(supplied, Pin);
    }

    /// <summary>
    /// Compares in constant time so a wrong guess can't be timed to learn the PIN one digit at
    /// a time. The PIN is only ever a six-digit fixed-length string, so a length mismatch alone
    /// is not worth hiding behind the same constant-time path.
    /// </summary>
    private static bool FixedTimeEquals(string a, string b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        int diff = 0;
        for (int i = 0; i < a.Length; i++)
        {
            diff |= a[i] ^ b[i];
        }

        return diff == 0;
    }

    private static async Task WriteAsync(HttpListenerResponse response, string contentType, byte[] bytes)
    {
        response.ContentType = contentType;
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
    }

    private static void TrySetStatus(HttpListenerResponse response, int statusCode)
    {
        try
        {
            response.StatusCode = statusCode;
        }
        catch (Exception)
        {
            // Headers may already be sent; nothing more to do.
        }
    }

    /// <summary>Every up, non-loopback, non-tunnel IPv4 address — the ones a phone could reach.</summary>
    private static IEnumerable<string> LocalIPv4Addresses()
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up ||
                nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
            {
                continue;
            }

            foreach (var info in nic.GetIPProperties().UnicastAddresses)
            {
                if (info.Address.AddressFamily != AddressFamily.InterNetwork)
                {
                    continue;
                }

                // 169.254.x.x is an unreachable auto-assigned fallback, not a usable LAN address.
                byte[] bytes = info.Address.GetAddressBytes();
                if (bytes[0] == 169 && bytes[1] == 254)
                {
                    continue;
                }

                yield return info.Address.ToString();
            }
        }
    }

    /// <summary>
    /// Fills in <see cref="PageHtml"/>'s handful of <c>__TOKEN__</c> placeholders for the
    /// requested language. Everything else — layout, CSS, the routing logic in the script — is
    /// one shared template; only the words and the <c>&lt;html&gt;</c> direction differ, the
    /// same split <see cref="MainForm.ApplyLanguage"/> keeps on the desktop side. RTL is safe
    /// to do properly here (unlike the owner-drawn desktop UI): this is plain CSS a browser
    /// mirrors on its own, not GDI+ that would need every glyph and icon redrawn by hand.
    /// </summary>
    private static string BuildPageHtml(Language language)
    {
        bool fa = language == Language.Persian;
        string brand = fa ? "کنترل از راه دور ClickerBot" : "ClickerBot Remote";

        var labels = new
        {
            noProfile = fa ? "بدون پروفایل" : "No profile",
            ready = fa ? "آماده" : "Ready",
            start = fa ? "شروع" : "Start",
            stop = fa ? "توقف" : "Stop",
            runningSuffix = fa ? " · در حال اجرا" : " · running",
            connected = fa ? "متصل" : "Connected",
            serverError = fa ? "خطای سرور" : "Server error",
            disconnected = fa ? "قطع شد" : "Disconnected",
            locked = fa ? "قفل" : "Locked",
            checking = fa ? "در حال بررسی…" : "Checking…",
            pinRejected = fa ? "پین رد شد — دوباره امتحان کنید." : "That PIN was rejected. Try again.",
            wrongPin = fa ? "پین اشتباه است — دوباره امتحان کنید." : "Wrong PIN — try again.",
            unreachable = fa ? "ارتباط با ClickerBot برقرار نشد." : "Could not reach ClickerBot.",
        };

        return PageHtml
            .Replace("__HTML_ATTRS__", fa ? "lang=\"fa\" dir=\"rtl\"" : "lang=\"en\"")
            .Replace("__TITLE__", brand)
            .Replace("__BRAND__", brand)
            .Replace("__LOCK_HEADING__", fa
                ? "پینی را که روی رایانه نمایش داده شده وارد کنید"
                : "Enter the PIN shown on your PC")
            .Replace("__LOCK_BODY__", fa
                ? "ClickerBot هر بار که اجرا می‌شود، یک پین 6 رقمی تازه در کارت ریموت نشان می‌دهد."
                : "ClickerBot shows a fresh 6-digit PIN in the Remote card each time it starts.")
            .Replace("__CONNECTING_LABEL__", fa ? "در حال اتصال…" : "Connecting…")
            .Replace("__LOADING_LABEL__", fa ? "در حال بارگذاری…" : "Loading…")
            .Replace("__START_LABEL__", labels.start)
            .Replace("__LABELS_JSON__", JsonSerializer.Serialize(labels));
    }

    // The phone-facing page. Self-contained — no external fonts, scripts, or images — so it
    // works with the phone on the LAN and nothing else. Visual language matches the desktop
    // app and its icon exactly: the same reticle-and-lamp mark, the same rule that colour
    // marks the running state and nothing else does. Built through BuildPageHtml, above, which
    // fills in the __TOKEN__ placeholders for the requested language.
    private const string PageHtml = """
    <!doctype html>
    <html __HTML_ATTRS__>
    <head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover, maximum-scale=1">
    <meta name="theme-color" content="#0B0B0D">
    <title>__TITLE__</title>
    <style>
      :root{
        --bg:#0B0B0D; --surface:#17171A; --border:#27272A; --borderStrong:#3F3F46;
        --text:#F4F4F5; --text2:#A1A1AA; --text3:#71717A;
        --accent:#FBBF24; --onaccent:#1A1200;
        --danger:#F87171; --ondanger:#2C0B0B;
        --success:#4ADE80;
      }
      *{box-sizing:border-box}
      html,body{margin:0;padding:0;background:var(--bg);color:var(--text);
        font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,sans-serif;
        height:100%;overscroll-behavior:none;-webkit-tap-highlight-color:transparent;user-select:none}
      .mono{font-family:ui-monospace,"SF Mono",Consolas,"Roboto Mono",monospace}

      .screen{min-height:100dvh;display:flex;flex-direction:column;
        padding:max(20px,env(safe-area-inset-top)) 20px max(20px,env(safe-area-inset-bottom))}

      .topbar{display:flex;align-items:center;justify-content:space-between;height:28px;flex:none}
      .brand{display:flex;align-items:center;gap:8px;font-size:13px;font-weight:600;color:var(--text2);
        letter-spacing:.2px}
      .brand svg{width:16px;height:16px}
      .conn{display:flex;align-items:center;gap:6px;font-size:11px;color:var(--text3)}
      .conn .dot{width:6px;height:6px;border-radius:50%;background:var(--text3)}
      .conn.live .dot{background:var(--success)}

      .hero{flex:1;display:flex;flex-direction:column;align-items:center;justify-content:center;
        gap:18px;padding:16px 0}
      .lamp-wrap{position:relative;width:132px;height:132px}
      .lamp-halo{position:absolute;inset:-26px;border-radius:50%;
        background:radial-gradient(closest-side, color-mix(in srgb, var(--accent) 55%, transparent), transparent 72%);
        opacity:0;transition:opacity .5s ease}
      .lamp-halo.on{opacity:1;animation:breathe 2.6s ease-in-out infinite}
      @keyframes breathe{0%,100%{opacity:.55}50%{opacity:1}}
      .lamp{position:relative;width:132px;height:132px;border-radius:30px;
        background:linear-gradient(180deg,#0B0B0D,#17171A);
        border:1px solid var(--border);display:flex;align-items:center;justify-content:center}
      .lamp svg{width:100%;height:100%}
      .lamp .dot{transition:fill .35s ease}

      .status-block{text-align:center}
      .profile-name{font-size:20px;font-weight:700;letter-spacing:-.2px}
      .status-line{margin-top:4px;font-size:13px;color:var(--text2);min-height:18px}
      .status-line .mono{color:var(--text2)}
      .readout{margin-top:10px;display:flex;gap:18px;justify-content:center;font-size:13px;color:var(--text3);
        min-height:18px}
      .readout b{color:var(--text2);font-weight:600}

      .action{flex:none;margin-top:auto}
      .action button{
        width:100%;border:none;border-radius:24px;padding:0;height:104px;
        font-size:22px;font-weight:700;letter-spacing:.3px;
        display:flex;align-items:center;justify-content:center;gap:10px;
        -webkit-appearance:none;appearance:none;cursor:pointer;
        transition:transform .12s ease, filter .12s ease, background .2s ease;
      }
      .action button:active{transform:scale(.97);filter:brightness(.94)}
      .action button.start{background:var(--accent);color:var(--onaccent)}
      .action button.stop{background:var(--danger);color:var(--ondanger)}
      .action button:disabled{opacity:.5;pointer-events:none}
      .hint{text-align:center;margin-top:12px;font-size:12px;color:var(--text3);min-height:16px}

      .lock{position:fixed;inset:0;background:var(--bg);display:flex;flex-direction:column;
        align-items:center;justify-content:center;gap:26px;padding:24px;z-index:20}
      .lock.hidden{display:none}
      .lock .mark{width:64px;height:64px}
      .lock h1{font-size:17px;font-weight:700;margin:0;text-align:center}
      .lock p{font-size:13px;color:var(--text2);margin:0;text-align:center;max-width:280px;line-height:1.5}
      /* PIN boxes fill left-to-right even on the Persian page — the same convention every
         OTP/PIN entry uses regardless of the surrounding language. */
      .pin-row{display:flex;gap:10px;direction:ltr}
      .pin-box{width:44px;height:56px;border-radius:12px;background:var(--surface);
        border:1.5px solid var(--border);color:var(--text);font-size:24px;text-align:center;
        font-family:ui-monospace,Consolas,monospace;caret-color:var(--accent)}
      .pin-box:focus{outline:none;border-color:var(--accent)}
      .pin-box.filled{border-color:var(--borderStrong)}
      .lock-error{font-size:12.5px;color:var(--danger);min-height:16px}

      @media (prefers-reduced-motion: reduce){
        .lamp-halo.on{animation:none;opacity:.85}
        .action button{transition:none}
      }
    </style>
    </head>
    <body>

    <div class="lock" id="lock">
      <svg class="mark" viewBox="0 0 100 100" fill="none">
        <rect width="100" height="100" rx="20" fill="url(#g1)"/>
        <g stroke="#E8E8EB" stroke-width="7" stroke-linecap="round">
          <path d="M22 30 V22 H30"/><path d="M78 30 V22 H70"/>
          <path d="M22 70 V78 H30"/><path d="M78 70 V78 H70"/>
        </g>
        <circle cx="50" cy="50" r="10" fill="#71717A"/>
        <defs><linearGradient id="g1" x1="50" y1="0" x2="50" y2="100">
          <stop offset="0" stop-color="#0B0B0D"/><stop offset="1" stop-color="#17171A"/>
        </linearGradient></defs>
      </svg>
      <h1>__LOCK_HEADING__</h1>
      <p>__LOCK_BODY__</p>
      <div class="pin-row" id="pinRow">
        <input class="pin-box" inputmode="numeric" pattern="[0-9]*" maxlength="1" autocomplete="off">
        <input class="pin-box" inputmode="numeric" pattern="[0-9]*" maxlength="1" autocomplete="off">
        <input class="pin-box" inputmode="numeric" pattern="[0-9]*" maxlength="1" autocomplete="off">
        <input class="pin-box" inputmode="numeric" pattern="[0-9]*" maxlength="1" autocomplete="off">
        <input class="pin-box" inputmode="numeric" pattern="[0-9]*" maxlength="1" autocomplete="off">
        <input class="pin-box" inputmode="numeric" pattern="[0-9]*" maxlength="1" autocomplete="off">
      </div>
      <div class="lock-error" id="lockError"></div>
    </div>

    <div class="screen">
      <div class="topbar">
        <div class="brand">
          <svg viewBox="0 0 100 100" fill="none">
            <rect width="100" height="100" rx="20" fill="#0B0B0D"/>
            <g stroke="#E8E8EB" stroke-width="9" stroke-linecap="round">
              <path d="M22 30 V22 H30"/><path d="M78 30 V22 H70"/>
              <path d="M22 70 V78 H30"/><path d="M78 70 V78 H70"/>
            </g>
            <circle cx="50" cy="50" r="12" fill="#FBBF24"/>
          </svg>
          __BRAND__
        </div>
        <div class="conn" id="conn"><span class="dot"></span><span id="connText">__CONNECTING_LABEL__</span></div>
      </div>

      <div class="hero">
        <div class="lamp-wrap">
          <div class="lamp-halo" id="halo"></div>
          <div class="lamp">
            <svg viewBox="0 0 100 100" fill="none">
              <g stroke="#E8E8EB" stroke-width="6.5" stroke-linecap="round">
                <path d="M24 32 V24 H32"/><path d="M76 32 V24 H68"/>
                <path d="M24 68 V76 H32"/><path d="M76 68 V76 H68"/>
              </g>
              <circle class="dot" id="lampDot" cx="50" cy="50" r="11" fill="#71717A"/>
            </svg>
          </div>
        </div>
        <div class="status-block">
          <div class="profile-name" id="profileName">—</div>
          <div class="status-line" id="statusLine">__LOADING_LABEL__</div>
          <div class="readout" id="readout"></div>
        </div>
      </div>

      <div class="action">
        <button id="actionBtn" class="start" disabled>__START_LABEL__</button>
        <div class="hint" id="hint">&nbsp;</div>
      </div>
    </div>

    <script>
    (() => {
      const L = __LABELS_JSON__;
      const $ = id => document.getElementById(id);
      const lampDot = $('lampDot'), halo = $('halo'), actionBtn = $('actionBtn'),
            statusLine = $('statusLine'), profileName = $('profileName'), readout = $('readout'),
            hint = $('hint'), conn = $('conn'), connText = $('connText'),
            lock = $('lock'), pinRow = $('pinRow'), lockError = $('lockError');

      let pin = localStorage.getItem('clickerbot-pin') || '';
      let busy = false;
      let lastRunning = false;

      function fmtElapsed(s){
        s = Math.max(0, Math.floor(s));
        const h = Math.floor(s/3600), m = Math.floor((s%3600)/60), sec = s%60;
        return h > 0 ? `${h}:${String(m).padStart(2,'0')}:${String(sec).padStart(2,'0')}`
                     : `${String(m).padStart(2,'0')}:${String(sec).padStart(2,'0')}`;
      }

      async function api(path, opts) {
        opts = opts || {};
        opts.headers = Object.assign({'X-Pin': pin}, opts.headers || {});
        return fetch(path, opts);
      }

      function renderStatus(s) {
        profileName.textContent = s.profileName || L.noProfile;
        lastRunning = s.running;
        lampDot.setAttribute('fill', s.running ? 'url(#lit)' : '#71717A');
        halo.classList.toggle('on', s.running);

        if (s.running) {
          statusLine.innerHTML = '<span class="mono">' + s.mode + '</span>' + L.runningSuffix;
          readout.innerHTML = `<span><b>${s.iteration.toLocaleString('en-US')}</b>${s.target ? ' / ' + s.target.toLocaleString('en-US') : ''}</span>` +
                               `<span class="mono">${fmtElapsed(s.elapsedSeconds)}</span>`;
          actionBtn.textContent = L.stop;
          actionBtn.className = 'stop';
        } else {
          statusLine.textContent = s.message || L.ready;
          readout.innerHTML = '';
          actionBtn.textContent = L.start;
          actionBtn.className = 'start';
        }
        actionBtn.disabled = false;
        conn.classList.add('live');
        connText.textContent = L.connected;
      }

      async function poll() {
        try {
          const res = await api('/api/status');
          if (res.ok) {
            renderStatus(await res.json());
          } else {
            conn.classList.remove('live');
            connText.textContent = L.serverError;
          }
        } catch (e) {
          conn.classList.remove('live');
          connText.textContent = L.disconnected;
        } finally {
          setTimeout(poll, 1500);
        }
      }

      actionBtn.addEventListener('click', async () => {
        if (busy) return;
        busy = true;
        actionBtn.disabled = true;
        const path = lastRunning ? '/api/stop' : '/api/start';
        try {
          const res = await api(path, { method: 'POST' });
          if (res.status === 401) {
            showLock(L.pinRejected);
            return;
          }
          hint.textContent = res.ok ? ' ' : await res.text();
        } catch (e) {
          hint.textContent = L.unreachable;
        } finally {
          busy = false;
          actionBtn.disabled = false;
        }
      });

      const boxes = Array.from(pinRow.children);
      function showLock(message) {
        lock.classList.remove('hidden');
        lockError.textContent = message || '';
        boxes.forEach(b => b.value = '');
        boxes[0].focus();
      }
      boxes.forEach((box, i) => {
        box.addEventListener('input', () => {
          box.value = box.value.replace(/\D/g, '').slice(0, 1);
          box.classList.toggle('filled', box.value.length === 1);
          if (box.value && i < boxes.length - 1) boxes[i+1].focus();
          if (boxes.every(b => b.value.length === 1)) submitPin();
        });
        box.addEventListener('keydown', (e) => {
          if (e.key === 'Backspace' && !box.value && i > 0) boxes[i-1].focus();
        });
      });

      async function submitPin() {
        const candidate = boxes.map(b => b.value).join('');
        lockError.textContent = L.checking;
        try {
          const res = await fetch('/api/verify', { method: 'POST', headers: { 'X-Pin': candidate } });
          if (res.ok) {
            pin = candidate;
            localStorage.setItem('clickerbot-pin', pin);
            lock.classList.add('hidden');
            lockError.textContent = '';
            poll();
          } else {
            lockError.textContent = L.wrongPin;
            boxes.forEach(b => b.value = '');
            boxes[0].focus();
          }
        } catch (e) {
          lockError.textContent = L.unreachable;
        }
      }

      const litGrad = document.createElementNS('http://www.w3.org/2000/svg', 'defs');
      litGrad.innerHTML = '<linearGradient id="lit" x1="50" y1="40" x2="50" y2="62">' +
        '<stop offset="0" stop-color="#FCD34D"/><stop offset="1" stop-color="#D97706"/></linearGradient>';
      document.querySelector('.lamp svg').appendChild(litGrad);

      if (pin) {
        lock.classList.add('hidden');
        poll();
      } else {
        showLock('');
        connText.textContent = L.locked;
      }
    })();
    </script>
    </body>
    </html>
    """;
}
