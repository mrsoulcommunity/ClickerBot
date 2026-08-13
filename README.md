<p align="center">
  <img src="ClickerBot/Assets/logo.png" width="96" alt="ClickerBot logo — a capture reticle around a lit indicator dot" />
</p>

<h1 align="center">ClickerBot</h1>

<p align="center">A Windows desktop automation tool that repeats a key press, a mouse click, or both — precisely, and on your terms.</p>

<p align="center">
  <img alt="Platform" src="https://img.shields.io/badge/platform-Windows%2010%2F11-0078D4?style=flat-square&logo=windows&logoColor=white">
  <img alt=".NET" src="https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet&logoColor=white">
  <img alt="UI" src="https://img.shields.io/badge/UI-Windows%20Forms-B45309?style=flat-square">
  <img alt="License" src="https://img.shields.io/badge/license-MIT-18181B?style=flat-square">
</p>

Built with .NET 8 and Windows Forms — every control is custom-drawn, so the interface looks the same whether you're on light or dark, and a live rhythm strip shows a run's actual pacing instead of just a spinning counter. 📱 It now also opens a page on your phone, so you can start and stop a run without walking back to the desk.

<p align="center">
  <img src="ClickerBot/Assets/screenshots/app-light.png" width="49%" alt="ClickerBot in light mode, idle, configured for a key-and-click run" />
  <img src="ClickerBot/Assets/screenshots/app-dark-running.png" width="49%" alt="ClickerBot in dark mode, mid-run — lit indicator lamp, live cadence strip, red Stop button" />
</p>

---

## Table of contents

- [✨ Features](#-features)
- [🖥️ Requirements](#️-requirements)
- [🚀 Getting started](#-getting-started)
- [🎯 Usage](#-usage)
- [📱 Mobile control](#-mobile-control)
- [🎨 Appearance](#-appearance)
- [🔷 Icon and logo](#-icon-and-logo)
- [⌨️ Hotkeys](#️-hotkeys)
- [🗂️ Profiles and data storage](#️-profiles-and-data-storage)
- [🕓 Run history](#-run-history)
- [🪟 Starting with Windows](#-starting-with-windows)
- [📁 Project structure](#-project-structure)
- [⚙️ How it works](#️-how-it-works)
- [📦 Building a standalone release](#-building-a-standalone-release)
- [🛠️ Troubleshooting](#️-troubleshooting)
- [⚠️ Responsible use](#️-responsible-use)
- [📄 License](#-license)

---

## ✨ Features

| Feature | Description |
| --- | --- |
| **Four action modes** | Key + click, key only, click only, or type text — the fields that don't apply to the chosen mode disable themselves rather than disappear, so the layout never jumps. |
| **Type text** | Types a fixed line via Unicode input rather than a single mapped key, so any character your keyboard layout can't reach — accents, symbols, other scripts — still works. Handy for repeated chat messages or form filling. |
| **Any key supported** | Letters, digits, function keys, `Tab`, `Enter`, `Space`, punctuation, arrows, and numpad keys. Press `Esc` in a key field to clear it. |
| **Left, right or middle click, single or double** | The mouse button and whether each iteration double-clicks are both configurable. |
| **Fixed point or the live cursor** | Click a captured screen coordinate, or click wherever the cursor already is so you can steer a run by hand. |
| **Click scatter** | A random pixel radius applied around the click target, so every click doesn't land on the exact same coordinate. |
| **Fixed or randomized delays** | Each delay is either a fixed millisecond value or a random value re-drawn from a `min–max` range on every iteration. |
| **Three ways to stop** | An iteration count, a time limit, or run until you stop it by hand. |
| **Start delay** | An optional countdown before the first action, so you have time to click into the target window. |
| **Test button** | Fires exactly one iteration right now — no start delay, no repeat count, nothing logged to history — so you can check a key, a click point, or typed text before committing to a full run. |
| **Pause and resume** | Hold a run in place without losing its iteration count or elapsed time, then continue exactly where it left off. |
| **Failsafe corner-abort** | Slamming the real cursor into any corner of the screen aborts a run immediately — a backstop for when a Stop hotkey couldn't be registered. On by default; turn it off in the *Window* card if a run legitimately needs to click near a corner. |
| **📱 Mobile control** | Enable it in the *Remote* card and ClickerBot serves a phone-friendly page on your LAN with a live status readout and a Start/Stop button — no cables, no companion app. PIN-protected. See [Mobile control](#-mobile-control). |
| **Live cadence strip** | A running strip of ticks, one per completed iteration, laid out on a real time axis — an even comb for a fixed delay, a ragged one for a random range. It's the fastest way to tell a run is behaving the way you configured it. |
| **Position capture** | Click **Pick** — or press its hotkey from anywhere — to store the current cursor position as the click target. |
| **Four global hotkeys** | Start, Pause, Stop, and Pick point are all registered system-wide and work while other applications have focus. All four are configurable per profile and must be distinct from each other and from the automated key. |
| **Profiles** | Create, rename, duplicate, delete, import, and export named configurations. Every setting is part of the profile. |
| **Import / export** | Share a profile file between machines, or keep a backup outside `%APPDATA%`, without touching the rest of your saved profiles. |
| **Run history** | The last 50 runs — profile, mode, when, how long, how many iterations, how it ended — kept in a themed dialog off the sidebar, so a run left going unattended has something to show for itself afterwards. |
| **Keep above other windows** | Optionally pins ClickerBot on top so the run panel stays visible over the window being automated. |
| **Hide to the notification area while running** | Drops the window out of the way for the duration of a run and restores it automatically when the run ends. The tray icon itself shows whether a run is active. |
| **Start with Windows** | Launches ClickerBot, minimized to the tray, when you sign in — toggled through the same per-user Run key Windows itself uses, no installer needed. |
| **Sound when you're not looking** | Plays a system sound when a run ends while the window is hidden or unfocused — silent if you're already watching it finish. |
| **Auto-save** | Changes are persisted to disk automatically a moment after you make them — nothing to remember to save. |
| **Light & dark themes** | One click on the header switch, with an animated transition. The title bar follows too. See [Appearance](#-appearance). |
| **High-DPI aware** | Per-monitor V2 DPI awareness, so the UI stays sharp on scaled and mixed-DPI displays. |

---

## 🖥️ Requirements

- **Windows 10 or Windows 11**
- **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** — required to build. Only the [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) is needed to run an already-built binary.

The app runs with normal user rights (`asInvoker`). See [Troubleshooting](#️-troubleshooting) if you need to drive an elevated window.

---

## 🚀 Getting started

### The easy way

1. Clone or download this repository.
2. Double-click **`Run.bat`** in the repository root.

That's it. On the first run `Run.bat` builds the project in Release mode (once), then launches the app. Every run after that starts the app immediately — no terminal required.

```bash
git clone https://github.com/mrsoulcommunity/ClickerBot.git
```

### The manual way

```bash
dotnet build ClickerBot/ClickerBot.csproj -c Release
```

```bash
dotnet run --project ClickerBot/ClickerBot.csproj -c Release
```

---

## 🎯 Usage

1. **Choose a mode** in the *Action* card: **Both** (key + click), **Key** (key only), **Click** (click only), or **Text** (types a line, no key or click). The fields that don't apply grey out.
2. **Pick a key, or type text** (whichever the mode uses). Click the **Key** field and press the key you want automated — or, in Text mode, type the line you want it to enter each iteration.
3. **Set up the click** (if the mode uses one): pick the button, whether it double-clicks, and whether it clicks **a fixed point** or **the cursor**'s live position. For a fixed point, move your cursor to the target and press the **Pick** hotkey (or click **Pick**), or type the X/Y coordinates directly. **Scatter** adds a random pixel radius around that point so clicks don't land on the exact same spot every time.
4. **Tune the timing** in the *Timing* card:
   - **After the key press, before the click** — only used by the Both mode.
   - **Between one iteration and the next.**
   - Tick **Random** on either one to draw a fresh value from a `min–max` range each iteration.
   - **Start delay** — an optional countdown before the first action, to give you time to click into the target window.
5. **Choose how the run ends** in the *Repeat* card: a **Count** of iterations, a **Duration**, or **Until stopped**.
6. **Try it once first.** The run bar's **Test** button fires exactly one iteration immediately, with no start delay and nothing recorded to history — the fastest way to confirm the key, click point, or typed text is right before committing to a real run.
7. **Press Start** — or your Start hotkey, or the Start button on your phone. The settings panel locks while a run is in progress; the cadence strip and readouts in the run bar update live.
8. **Pause and resume** with the run bar's Pause button or your Pause hotkey — the iteration count and elapsed time hold in place and continue from there.
9. **Press Stop** — or your Stop hotkey, or your phone, or move the mouse into a screen corner — to cancel at any time.

The switch in the top-right corner flips between light and dark at any time; see [Appearance](#-appearance). Past runs are one click away in **History**, at the bottom of the sidebar.

> **Note**
> The automated key cannot be one of the four hotkeys (this doesn't apply to Text mode, which has no key). Synthesized key presses trigger registered hotkeys just like real ones, so the run would stop itself, restart itself, pause itself, or quietly move the click target out from under itself. The app blocks these combinations and tells you which one you hit.

---

## 📱 Mobile control

Tick **Enable mobile control** in the *Remote* card and ClickerBot starts a small web server of its own — no separate install, no account, no cloud in between. It hosts a single page built for a phone: a big lamp that mirrors the desktop's own idle/running indicator, live iteration and elapsed-time readouts, and one button that reads **Start** or **Stop** depending on what's currently true.

<p align="center">
  <img src="ClickerBot/Assets/screenshots/remote-lock.png" width="32%" alt="Mobile control page — PIN entry screen" />
  <img src="ClickerBot/Assets/screenshots/remote-idle.png" width="32%" alt="Mobile control page — idle, ready to start" />
  <img src="ClickerBot/Assets/screenshots/remote-running.png" width="32%" alt="Mobile control page — a run in progress, lamp lit" />
</p>

**To connect:** tick the checkbox. A URL and a 6-digit PIN appear directly under it — click that line to copy the URL to your clipboard. Open the URL in your phone's browser (both devices need to be on the same Wi-Fi network), enter the PIN once, and you're in; the page remembers it for next time.

**What it can and can't do:** the page can start a run, stop it, and watch it happen — the same profile and settings you last configured on the desktop. It can't change any setting, switch profiles, or configure a new run; for that you're still at the keyboard. That split is deliberate: the phone is a remote for a run you already set up, not a second cockpit.

**On security, honestly:** the PIN is regenerated every time the server starts, and it gates every action that changes state — starting, stopping, even confirming the PIN itself. That's enough to stop a random device on the same Wi-Fi from touching it by accident, but it's LAN-toy security, not a hardened login: there's no rate limiting or account lockout, and the read-only status page doesn't require the PIN at all. Treat it like any other local-network convenience feature — fine for your own home or office Wi-Fi, not something to expose past your router.

**No firewall prompts, no admin rights.** ClickerBot binds the server to `127.0.0.1` plus each of your machine's real LAN addresses individually, rather than to a wildcard address — the wildcard is what actually requires elevated permissions on Windows, so this feature needs none.

---

## 🎨 Appearance

The switch in the top-right corner of the header toggles between the light and dark themes. The change is immediate and animated, and it covers the whole window — including the title bar, which is repainted through the Desktop Window Manager rather than left as a light strip above a dark app.

- **First run** starts on whatever appearance Windows itself is set to, read from `AppsUseLightTheme`.
- **Your choice is remembered** in the same file as your profiles, as an application-wide setting. Switching profiles never changes the theme.
- **Nothing else changes.** Themes are purely visual; every automation setting is untouched.

### How theming is built

`Theme` exposes the active `Palette` and raises `Changed` when it is swapped. Controls read colors inside `OnPaint` rather than caching them at construction, so a switch is mostly just a repaint. Stock WinForms controls do cache their colors, so those are wrapped in themed variants that implement `IThemedControl`, and `ThemeManager` walks the control tree calling `ApplyTheme()` on each one — with painting suspended so the change lands in a single frame.

Three controls are drawn from scratch instead of using the framework versions, because Windows paints those itself and they stay light no matter what colors are assigned: the checkbox glyph, the numeric field's spin buttons, and the confirmation dialog. Adding a third appearance would mean adding one `Palette` instance and changing nothing else.

---

## 🔷 Icon and logo

The mark is a capture reticle — four corner brackets, echoing the app's own Pick-point feature — around a single indicator dot. The reticle never changes; only the dot does, exactly matching the run panel's own rule that color marks the running state and nothing else does. Idle, the dot is a flat grey. Running, it lights amber with a soft glow. The mobile page's own hero lamp is the same mark, so a run looks like the same run whichever screen you're watching it from.

| Where | State | Source |
| --- | --- | --- |
| The compiled `.exe`'s own file icon (Explorer, the taskbar shortcut before launch, Alt-Tab) | Always idle — nothing is running yet | `ClickerBot/Assets/AppIcon.ico`, wired in via `<ApplicationIcon>` |
| The window's title bar and taskbar icon | Idle / running, live | Drawn at runtime by `AppIcon.cs` |
| The notification-area icon while hidden to tray | Idle / running, live | Same `AppIcon.cs` |
| This README | The lit mark | `ClickerBot/Assets/logo.png` |

The runtime copy exists because the compiled icon can't relight itself — nothing is running when Explorer shows it. `AppIcon.cs` draws the identical mark with GDI+ instead of loading a raster asset, so it can swap the dot's color the instant a run starts or stops, the same way every other themed control in the app repaints instead of being replaced.

---

## ⌨️ Hotkeys

| Hotkey | Action | Configurable |
| --- | --- | --- |
| `F7` | Start the current profile | Yes — per profile |
| `F8` | Pause or resume the current run | Yes — per profile |
| `F9` | Stop the current run | Yes — per profile |
| `F10` | Capture the cursor position as the click target | Yes — per profile |

Hotkeys are registered globally through the Win32 `RegisterHotKey` API, so they fire even when ClickerBot is in the background — including while it's hidden to the notification area. If another application already owns a key, the status bar tells you which binding could not be registered — pick a different one.

All four hotkeys must be distinct from each other, and from the automated key when the current mode uses one.

**If a hotkey can't be registered, the failsafe still works.** Moving the real mouse cursor into any corner of the screen aborts a run immediately, whether or not the Stop hotkey is available — see [Features](#-features). It's on by default and can be turned off per your preference in the *Window* card.

---

## 🗂️ Profiles and data storage

Profiles hold every configurable value: the mode, key or typed text, mouse button, click target and scatter, both delay settings, the start delay, the repeat mode, and all four hotkeys. Switching profiles re-registers that profile's hotkeys immediately. The chosen theme and the window options (always-on-top, hide-to-tray, the failsafe toggle, mobile control) are stored alongside them but apply to the whole application rather than to a single profile.

Everything is stored as indented JSON at:

```
%APPDATA%\ClickerBot\profiles.json
```

The file is written automatically shortly after any change and again on exit. Each save is written alongside the real file and swapped in, so an interrupted write cannot leave a truncated file where your profiles were. If the file is ever missing or corrupt, the app falls back to a single fresh **Default** profile rather than failing to start; values outside the ranges the inputs allow are pulled back into range on load, so hand-editing it cannot put the app into a state it will not run.

**Import** reads profiles from another JSON file — one you exported earlier, or another machine's `profiles.json` — and adds them to your existing list under unique names, so importing never overwrites what's already there. **Export** writes your current profiles to a file you choose, without the application-wide settings, so it's portable between machines.

The app was previously called ClickerApp. If nothing is found at the path above, profiles are read once from the old `%APPDATA%\ClickerApp\profiles.json` and saved forward to the new location, so an existing setup carries over on its own. The old folder is left in place and can be deleted whenever you like.

---

## 🕓 Run history

The **History** button at the bottom of the sidebar opens a list of the last 50 runs: which profile, which mode, when it started, how long it ran, how many iterations it completed, and how it ended — Finished, Stopped, a failsafe abort, or an error. A one-shot **Test** doesn't get an entry; it isn't a run.

History is stored separately from your profiles, at `%APPDATA%\ClickerBot\history.json`, with the same crash-safe save as `profiles.json`. **Clear history** in the dialog empties the list — there's a confirmation first, and it can't be undone.

---

## 🪟 Starting with Windows

**Start ClickerBot when Windows starts**, in the *Window* card, adds ClickerBot to your per-user startup programs — the same `HKCU\...\CurrentVersion\Run` registry key Windows' own Settings app manages, so no installer or scheduled task is involved and nothing needs administrator rights.

It launches with a `--minimized` flag that sends the window straight to the notification area instead of popping open on every sign-in — the app is fully running (hotkeys included) from the moment you sign in, it just isn't in your way. Open it any time from the tray icon.

The checkbox reads the registry directly rather than a saved preference, so it always shows whether autostart is *actually* registered, not just whether it was the last time you asked.

---

## 📁 Project structure

```
.
├── ClickerBot/
│   ├── Assets/
│   │   ├── AppIcon.ico            # Compiled .exe's file icon (idle state, multi-resolution)
│   │   ├── logo.png               # README art (lit state)
│   │   ├── logo-idle.png          # README art (idle state)
│   │   └── screenshots/           # README screenshots (app + mobile control)
│   ├── Automation/
│   │   ├── AutomationRunner.cs   # The async action loop: mode-aware, pausable, time-or-count bounded
│   │   └── RunProgress.cs        # Run phase snapshot + the pause gate the UI holds
│   ├── Input/
│   │   ├── HotkeyManager.cs      # Global hotkey registration (RegisterHotKey)
│   │   ├── KeyNames.cs           # Human-readable key labels
│   │   ├── NativeInput.cs        # SendInput wrapper for synthetic key/mouse/text input
│   │   └── StartupManager.cs     # Reads/writes the per-user Run registry key
│   ├── Models/
│   │   ├── ActionMode.cs         # Mode / button / repeat-mode / click-target enums
│   │   ├── DelaySetting.cs       # Fixed or random-range delay
│   │   ├── Limits.cs             # The valid range of every numeric setting
│   │   ├── Profile.cs            # One named configuration
│   │   ├── ProfileStore.cs       # JSON load/save, plus import/export
│   │   └── RunHistory.cs         # A finished run's record, and its JSON load/save
│   ├── Remote/
│   │   ├── RemoteControlServer.cs   # Local HTTP server: routing, PIN auth, the mobile page itself
│   │   └── RemoteStatusPayload.cs   # The status snapshot served to the phone
│   ├── Ui/
│   │   ├── Controls/
│   │   │   ├── CadenceMeter.cs     # Live per-iteration rhythm strip
│   │   │   ├── Card.cs             # Titled section container
│   │   │   ├── ConfirmDialog.cs    # Themed replacement for MessageBox
│   │   │   ├── DelayEditor.cs      # Fixed/random delay control
│   │   │   ├── FlatButton.cs       # Owner-drawn button (primary/secondary/danger)
│   │   │   ├── KeyCaptureBox.cs    # Field that records the next key pressed
│   │   │   ├── NumberBox.cs        # Owner-drawn numeric field with steppers
│   │   │   ├── ProfileListBox.cs   # Owner-drawn profile list
│   │   │   ├── RunHistoryDialog.cs # Themed dialog listing past runs
│   │   │   ├── RunPanel.cs         # Run bar: status, readouts, cadence strip, transport
│   │   │   ├── Segmented.cs        # Owner-drawn segmented choice control
│   │   │   ├── SurfacePanel.cs     # Panel that tracks the surface color
│   │   │   ├── TextField.cs        # Owner-drawn free-text field (the Type-text mode's input)
│   │   │   ├── ThemeToggle.cs      # Animated light/dark switch
│   │   │   ├── ThemedCheckBox.cs   # Owner-drawn checkbox
│   │   │   ├── ThemedLabel.cs      # Label that stores a role, not a color
│   │   │   └── ThemedTextBox.cs    # Palette-aware text box
│   │   ├── Theming/
│   │   │   ├── Palette.cs          # One complete set of colors
│   │   │   ├── Theme.cs            # Active palette, fonts, drawing helpers
│   │   │   ├── ThemeManager.cs     # Pushes the theme through the control tree
│   │   │   ├── ThemeMode.cs        # Light / Dark
│   │   │   └── WindowChrome.cs     # Dark title bar + system preference
│   │   ├── AppIcon.cs             # Drawn window/tray icon, amber when a run is active
│   │   ├── MainForm.cs            # Wiring: profiles, settings, hotkeys, run control, tray, remote server
│   │   ├── MainForm.Layout.cs     # Where every control is placed
│   │   └── UiFactory.cs           # Small control factory helpers
│   ├── app.manifest              # DPI awareness, execution level, OS support
│   ├── ClickerBot.csproj
│   └── Program.cs                # Entry point; handles the --minimized launch flag
├── .gitattributes                # Line-ending rules (CRLF for .bat)
├── .gitignore
├── LICENSE                       # MIT
├── README.md
└── Run.bat                       # One-click build-and-launch script
```

---

## ⚙️ How it works

`AutomationRunner.RunAsync` drives the loop while the UI thread stays responsive. Each iteration is one call to `PerformIterationAsync`, which switches on the mode:

- **Both** — synthesize a key-down/key-up pair via `SendInput`, wait the *after key press* delay, then click.
- **Key** — synthesize the key-down/key-up pair; no click, no gap to wait out.
- **Click** — click, with no key involved.
- **Text** — synthesize one `SendInput` Unicode event pair per character, bypassing virtual-key mapping entirely so any character your keyboard layout can't reach still types correctly.

A click moves the cursor first, unless the target is the live cursor position, then synthesizes the configured button — twice, for a double-click. A **scatter** radius, if set, offsets the point by a random amount inside that radius on every click, sampled evenly across the disc rather than bunched toward the center. The **Test** button calls the very same `PerformIterationAsync` directly, once, outside the loop — there is exactly one place that knows how to perform an iteration, and both the real run and the one-shot test call it.

Around that iteration, the loop:

1. Waits out the start delay, if any, reporting a countdown each second (skipped entirely for a test).
2. Checks the stop condition — iteration count or elapsed duration — before each iteration.
3. Reports progress to the UI and marks the iteration on the cadence strip.
4. Waits the *between iterations* delay, then repeats.

Each wait calls `DelaySetting.Next()`, so a randomized delay produces a different value on every pass. Cancellation is cooperative through a `CancellationTokenSource`, checked before each synthesized input and honored by every `Task.Delay`, so **Stop** — including a failsafe-triggered one — takes effect within one delay interval at most.

**Pause** goes through a separate `PauseGate` rather than cancellation: the loop parks on it between iterations, and the elapsed-time clock stops with it, so a duration-limited run doesn't burn its budget while paused. Resuming picks the loop back up with its iteration count and remaining time exactly where they were.

**The failsafe** is checked outside `AutomationRunner` entirely, on the same 100ms UI timer that already repaints the cadence strip: if the real cursor is within a pixel of any corner of `SystemInformation.VirtualScreen`, the run is cancelled with a reason that overrides the ordinary "Stopped" message — unless that corner happens to be the profile's own configured click point, since a run is allowed to legitimately park the cursor there between clicks.

**Mobile control** runs on `System.Net.HttpListener`, bound to `127.0.0.1` and each of the machine's real LAN IPv4 addresses individually — never a wildcard prefix, which is what would require administrator rights on Windows. `MainForm` hands it three callbacks (`GetStatus`, `RequestStart`, `RequestStop`); since the listener answers requests on its own background threads, every one of those callbacks marshals back onto the UI thread — `Invoke` for the status read, which needs a return value, `BeginInvoke` for start/stop, which don't. The PIN is regenerated with `RandomNumberGenerator` each time the server starts and compared in constant time, so a failed guess can't be timed to narrow down the right one.

Extended keys (arrows, `Insert`, `Delete`, `Home`, `End`, `Page Up`/`Page Down`, numpad `/`, right `Ctrl`/`Alt`, and others) are flagged with `KEYEVENTF_EXTENDEDKEY` so target applications receive them correctly.

---

## 📦 Building a standalone release

To produce a single self-contained executable that runs without the .NET runtime installed:

```bash
dotnet publish ClickerBot/ClickerBot.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The result lands in `ClickerBot/bin/Release/net8.0-windows/win-x64/publish/`.

For a much smaller build that requires the .NET 8 Desktop Runtime on the target machine:

```bash
dotnet publish ClickerBot/ClickerBot.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

---

## 🛠️ Troubleshooting

**A hotkey does nothing, and the status bar mentions another app.**
Windows grants a global hotkey to the first application that asks for it. Another running program owns that key — choose a different one in the *Hotkeys* card.

**"Could not move the cursor … " or "SendInput failed … Input may be blocked by an elevated window."**
Windows blocks synthetic input from a normal-rights process to a process running as administrator. Either run the target application without elevation, or run ClickerBot as administrator. To make elevation permanent, change `asInvoker` to `requireAdministrator` in [`ClickerBot/app.manifest`](ClickerBot/app.manifest) and rebuild.

**The click lands in the wrong place.**
Coordinates are absolute screen pixels across the whole virtual desktop. Re-capture the position with the Pick hotkey if you changed your display scaling, resolution, or monitor arrangement — or set **Click at** to **The cursor** if you'd rather aim it by hand each time.

**`Run.bat` reports that the .NET SDK was not found.**
Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), then run the file again.

**My settings disappeared.**
Check that `%APPDATA%\ClickerBot\profiles.json` exists and is readable. A corrupt file is ignored on startup and replaced with a fresh default profile. Run history lives in a separate `history.json` next to it and follows the same rule.

**A run keeps stopping itself the moment it starts, saying the mouse touched a corner.**
The profile's own click point is a screen corner (or close to one), and something outside the run nudged the cursor before the automation could park it there itself. Either move the click target away from the corner, or turn off **Abort if the mouse touches a screen corner** in the *Window* card for that profile.

**Starting with Windows is checked, but ClickerBot didn't launch at sign-in.**
Some third-party startup managers and enterprise policies clear entries from the per-user Run key. Re-check the box, or add ClickerBot through your startup manager pointing at the ClickerBot executable with a `--minimized` argument.

**My phone can't reach the mobile control page.**
Both devices need to be on the same Wi-Fi network — the server only binds to your machine's real LAN addresses, not the public internet. Check the URL shown under the checkbox in the *Remote* card was copied correctly, and that Windows Firewall isn't blocking ClickerBot on a network you've marked Public (accept the firewall prompt, or allow it manually for Private/Domain networks).

**The mobile page asks for the PIN again after it worked before.**
The PIN is regenerated every time the server starts — including every time you toggle *Enable mobile control* off and back on, or restart ClickerBot. Read the new one off the *Remote* card.

---

## ⚠️ Responsible use

This tool synthesizes real keyboard and mouse input at the operating-system level. Use it for legitimate automation — repetitive data entry, testing your own software, accessibility assistance, and similar tasks. Many online games and web services prohibit automated input in their terms of service, and using this tool against them may get your account suspended. You are responsible for how you use it.

---

## 📄 License

Released under the [MIT License](LICENSE). You are free to use, modify, and distribute this software, including commercially, provided the copyright notice and license text are retained. The software is provided as-is, without warranty of any kind.
