namespace ClickerBot;

/// <summary>
/// Every piece of interface text, in both languages. Mirrors <see cref="Theme"/>: controls
/// read these properties instead of caching literals, <see cref="Changed"/> fires after
/// <see cref="Apply"/> switches languages, and <see cref="MainForm.ApplyLanguage"/> is the
/// tree-walk that pushes the new strings out — the wording equivalent of a repaint.
///
/// Deliberately text-only. Numbers, dates and hotkey names (F7, Enter, Backspace…) stay in
/// Western digits and Latin key names in both languages, the same way <see cref="NumberBox"/>
/// always parses with <see cref="System.Globalization.CultureInfo.InvariantCulture"/> — a
/// measurement or a key your keyboard prints on it is not prose to translate.
/// </summary>
internal static class Loc
{
    public static event Action? Changed;

    public static Language Current { get; private set; } = SystemPreference();

    public static bool IsPersian => Current == Language.Persian;

    public static void Apply(Language language)
    {
        if (Current == language)
        {
            return;
        }

        Current = language;
        Changed?.Invoke();
    }

    /// <summary>Persian if that is what Windows itself is set to display; English otherwise.</summary>
    public static Language SystemPreference() =>
        System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            .Equals("fa", StringComparison.OrdinalIgnoreCase)
            ? Language.Persian
            : Language.English;

    // A right-to-left embedding. The window's own layout stays left-to-right in both languages,
    // which means Windows resolves each Persian string against a left-to-right base direction —
    // and under that base direction the bidirectional algorithm puts a trailing full stop at the
    // wrong end of the sentence, and moves a comma that sits next to an embedded Latin word
    // ("…ویندوز، ClickerBot…") across it. The text is correct and the ordering is not.
    //
    // Wrapping each translated string in RLE…PDF gives it its own right-to-left base direction
    // without touching the layout around it, which is exactly the scope of the problem. Doing it
    // here rather than at each control means it applies everywhere at once — owner-drawn text,
    // stock labels, the tray menu — and stays correct for strings assembled later, since the
    // pair is balanced and safe to concatenate.
    private const string Rle = "‫";
    private const string Pdf = "‬";

    private static string T(string english, string persian) =>
        Current == Language.Persian ? Rle + persian + Pdf : english;

    /// <summary>
    /// A translation that is stored as data rather than only displayed — a profile's name ends
    /// up in profiles.json — so it is returned bare. Invisible ordering marks belong in a label,
    /// not in a name the user typed alongside and can export. Every string routed through here
    /// is plain Persian with no trailing punctuation and no embedded Latin, which is precisely
    /// the case the bidirectional algorithm already handles correctly without help.
    /// </summary>
    private static string Name(string english, string persian) =>
        Current == Language.Persian ? persian : english;

    // --- App-wide ----------------------------------------------------------

    public static string AppTitle => "ClickerBot";

    public static string RenameHint => T("Type in the name above to rename", "برای تغییر نام، همین بالا بنویسید");

    public static string Cancel => T("Cancel", "لغو");

    public static string SwitchToLightMode => T("Switch to light mode", "رفتن به حالت روشن");

    public static string SwitchToDarkMode => T("Switch to dark mode", "رفتن به حالت تاریک");

    public static string SwitchLanguage => T("Switch language", "تغییر زبان");

    // --- Sidebar / profiles --------------------------------------------------

    public static string ProfilesCaption => T("PROFILES", "پروفایل‌ها");

    /// <summary>Both the button's label and the name a newly created profile is given.</summary>
    public static string NewProfile => Name("New profile", "پروفایل جدید");

    public static string Duplicate => T("Duplicate", "کپی");

    public static string Delete => T("Delete", "حذف");

    public static string Import => T("Import", "ایمپورت");

    public static string Export => T("Export", "اکسپورت");

    public static string History => T("History", "تاریخچه");

    public static string DefaultProfileName => Name("Default", "پیش‌فرض");

    public static string UntitledProfileName => Name("Untitled", "بی‌نام");

    /// <summary>Appended to a profile's own name to build its duplicate's default name.</summary>
    public static string CopySuffix => Name(" copy", " کپی");

    // --- Action card -----------------------------------------------------

    public static string ActionCardTitle => T("ACTION", "عملیات");

    public static string ModeLabel => T("Mode", "حالت");

    public static string[] ModeItems => IsPersian
        ? new[] { "هردو", "کلید", "کلیک", "متن" }
        : new[] { "Both", "Key", "Click", "Text" };

    public static string KeyWord => T("Key", "کلید");

    public static string TextWord => T("Text", "متن");

    public static string KeyPlaceholder => T("Click, then press a key", "کلیک کنید، سپس کلیدی بزنید");

    public static string EscClears => T("Esc clears", "پاک‌کردن با Esc");

    public static string ButtonLabel => T("Button", "دکمه");

    public static string[] MouseButtonItems => IsPersian
        ? new[] { "چپ", "راست", "وسط" }
        : new[] { "Left", "Right", "Middle" };

    public static string DoubleClick => T("Double", "دوبار");

    public static string ClickAtLabel => T("Click at", "محل کلیک");

    public static string[] ClickTargetItems => IsPersian
        ? new[] { "نقطه ثابت", "مکان‌نما" }
        : new[] { "A fixed point", "The cursor" };

    public static string PointLabel => T("Point", "نقطه");

    public static string Pick => T("Pick", "انتخاب");

    public static string ScatterLabel => T("Scatter", "پراکندگی");

    public static string ScatterHint => T("px of random spread per click", "پیکسل پراکندگی هر کلیک");

    // --- Timing card -------------------------------------------------------

    public static string TimingCardTitle => T("TIMING", "زمان‌بندی");

    public static string KeyDelayHint =>
        T("After the key press, before the click", "بعد از فشردن کلید، پیش از کلیک");

    public static string ClickDelayHint =>
        T("Between one iteration and the next", "بین هر تکرار و تکرار بعدی");

    public static string StartDelayLabel => T("Start delay", "تأخیر شروع");

    public static string StartDelayHint =>
        T("seconds before the first action", "ثانیه پیش از نخستین اجرا");

    public static string RandomCheck => T("Random", "تصادفی");

    public static string DelayTo => T("to", "تا");

    // --- Repeat card -------------------------------------------------------

    public static string RepeatCardTitle => T("REPEAT", "تکرار");

    public static string[] RepeatItems => IsPersian
        ? new[] { "تعداد", "مدت‌زمان", "تا توقف" }
        : new[] { "Count", "Duration", "Until stopped" };

    public static string IterationsLabel => T("Iterations", "تعداد تکرار");

    public static string MinutesLabel => T("Minutes", "دقیقه");

    public static string RepeatHintCount =>
        T("The run ends once it has done this many iterations.", "اجرا پس از این تعداد تکرار پایان می‌یابد.");

    public static string RepeatHintDuration => T(
        "The run ends after this long. Paused time does not count.",
        "اجرا پس از این مدت پایان می‌یابد؛ زمان توقف موقت حساب نمی‌شود.");

    public static string RepeatHintForever =>
        T("The run keeps going until you stop it.", "اجرا تا زمانی‌که متوقفش نکنید ادامه می‌یابد.");

    // --- Hotkeys card ------------------------------------------------------

    public static string HotkeysCardTitle => T("HOTKEYS", "کلیدهای میانبر");

    public static string NotSet => T("Not set", "تنظیم‌نشده");

    public static string PressAnyKey => T("Press any key…", "کلیدی را فشار دهید…");

    // --- Window card -------------------------------------------------------

    public static string WindowCardTitle => T("WINDOW", "پنجره");

    public static string AlwaysOnTop => T("Keep above other windows", "همیشه بالای سایر پنجره‌ها");

    public static string HideToTray =>
        T("Hide to the notification area while running", "هنگام اجرا به ناحیهٔ اعلان‌ها مخفی شود");

    public static string AutoStart =>
        T("Start ClickerBot when Windows starts", "با روشن‌شدن ویندوز، ClickerBot اجرا شود");

    public static string Failsafe =>
        T("Abort if the mouse touches a screen corner", "اگر ماوس به گوشهٔ صفحه برسد، لغو شود");

    // --- Remote card ---------------------------------------------------------

    public static string RemoteCardTitle => T("REMOTE", "ریموت");

    public static string RemoteEnabledCheck => T("Enable mobile control", "فعال‌سازی کنترل با موبایل");

    public static string RemoteOff => T("Off", "خاموش");

    public static string RemoteCopiedMessage =>
        T("Copied the remote address to the clipboard", "نشانی ریموت در کلیپ‌بورد کپی شد");

    public static string CouldNotStartRemoteServer(int port, string reason) => T(
        $"Could not start the remote server on port {port}: {reason}",
        $"راه‌اندازی سرور ریموت روی پورت {port} ممکن نشد: {reason}");

    // --- Run panel -----------------------------------------------------------

    public static string Ready => T("Ready", "آماده");

    public static string Test => T("Test", "آزمایش");

    public static string Start => T("Start", "شروع");

    public static string Pause => T("Pause", "توقف موقت");

    public static string Resume => T("Resume", "ادامه");

    public static string Stop => T("Stop", "توقف");

    public static string PickPoint => T("Pick point", "انتخاب نقطه");

    public static string IterationsCaption => T("ITERATIONS", "تکرار");

    public static string ElapsedCaption => T("ELAPSED", "گذشته");

    public static string RateCaption => T("RATE", "نرخ");

    public static string Running => T("Running", "در حال اجرا");

    /// <summary>The status word — "Paused" — as distinct from <see cref="Pause"/>, the button's
    /// verb. Persian uses the same phrase for both; English does not.</summary>
    public static string PausedStatus => T("Paused", "توقف موقت");

    public static string StartingIn(int seconds) => T($"Starting in {seconds}…", $"شروع در {seconds}…");

    // --- Run outcomes ----------------------------------------------------
    //
    // MainForm always builds and stores the English form (RunHistoryEntry.Outcome, the phone
    // API's "message" field) so history.json and any string matching on it — see
    // HistoryListBox — never depend on which language was active when a run finished. This is
    // the one place that turns the stored English back into words for display.

    public const string OutcomeFinished = "Finished";
    public const string OutcomeTestComplete = "Test complete";
    public const string OutcomeStopped = "Stopped";
    public const string OutcomeFailsafeStopped = "Stopped — the mouse touched a screen corner";

    public static string DescribeOutcome(string canonical) => canonical switch
    {
        OutcomeFinished => T("Finished", "پایان یافت"),
        OutcomeTestComplete => T("Test complete", "آزمایش انجام شد"),
        OutcomeStopped => T("Stopped", "متوقف شد"),
        OutcomeFailsafeStopped => T(
            "Stopped — the mouse touched a screen corner", "متوقف شد — ماوس به گوشهٔ صفحه برخورد کرد"),
        _ => canonical,
    };

    // --- Profile actions and messages -----------------------------------

    public static string CreatedProfile(string name) => T($"Created \"{name}\"", $"\"{name}\" ساخته شد");

    public static string DuplicatedProfile(string name) =>
        T($"Duplicated to \"{name}\"", $"با نام \"{name}\" کپی شد");

    public static string AtLeastOneProfileRequired =>
        T("At least one profile is required", "دست‌کم به یک پروفایل نیاز است");

    public static string DeleteProfileTitle => T("Delete profile", "حذف پروفایل");

    public static string DeleteProfileMessage(string name) => T(
        $"\"{name}\" will be removed. This cannot be undone.",
        $"\"{name}\" حذف خواهد شد. این کار قابل بازگشت نیست.");

    public static string DeletedProfile(string name) => T($"Deleted \"{name}\"", $"\"{name}\" حذف شد");

    public static string ImportDialogTitle => T("Import profiles", "ایمپورت پروفایل‌ها");

    public static string ExportDialogTitle => T("Export profiles", "اکسپورت پروفایل‌ها");

    // Bare, like the profile names: these are spliced into an OpenFileDialog's pipe-delimited
    // Filter string and rendered by the system dialog, which does its own right-to-left handling.
    public static string ProfilesFilterName => Name("ClickerBot profiles", "پروفایل‌های ClickerBot");

    public static string AllFilesFilterName => Name("All files", "همهٔ فایل‌ها");

    public static string NoProfilesInFile =>
        T("That file does not contain any profiles", "آن فایل هیچ پروفایلی ندارد");

    public static string ImportedProfiles(int count) => T(
        count == 1 ? "Imported 1 profile" : $"Imported {count} profiles",
        $"{count} پروفایل ایمپورت شد");

    public static string ExportedProfiles(int count) =>
        T($"Exported {count} profiles", $"{count} پروفایل اکسپورت شد");

    public static string CouldNotWriteFile(string reason) =>
        T($"Could not write that file: {reason}", $"نوشتن آن فایل ممکن نشد: {reason}");

    public static string ClickPointSet(int x, int y) =>
        T($"Click point set to {x}, {y}", $"محل کلیک روی {x}, {y} تنظیم شد");

    public static string BlockerNoText => T(
        "Type something first: click the text field and enter what to type",
        "اول متنی بنویسید: روی کادر متن کلیک کنید و آنچه باید تایپ شود را بنویسید");

    public static string BlockerNoKey => T(
        "Pick a key first: click the key field, then press any key",
        "اول کلیدی انتخاب کنید: روی کادر کلید کلیک کنید، سپس کلیدی را فشار دهید");

    public static string KeyIsHotkey(string keyName, string hotkeyName) => T(
        $"{keyName} is also the {hotkeyName} hotkey — choose another key",
        $"{keyName} هم‌زمان کلید میانبر {hotkeyName} است — کلید دیگری انتخاب کنید");

    public static string CouldNotChangeStartup(string reason) => T(
        $"Could not change startup setting: {reason}", $"تغییر تنظیم اجرای خودکار ممکن نشد: {reason}");

    public static string HotkeyClash(string keyName, string hotkeyName) =>
        T($"{keyName} is already the {hotkeyName} hotkey", $"{keyName} از پیش کلید میانبر {hotkeyName} است");

    public static string HotkeyUnavailable(string names) =>
        T($"Already used by another app: {names}", $"پیش‌تر توسط برنامه‌ای دیگر استفاده شده: {names}");

    public static string CouldNotSaveProfiles(string reason) =>
        T($"Could not save profiles: {reason}", $"ذخیرهٔ پروفایل‌ها ممکن نشد: {reason}");

    // --- Tray --------------------------------------------------------------

    public static string TrayIdle => "ClickerBot";

    public static string TrayRunning(string profileName) =>
        T($"ClickerBot — running \"{profileName}\"", $"ClickerBot — در حال اجرای \"{profileName}\"");

    public static string TrayShow => T("Show ClickerBot", "نمایش ClickerBot");

    public static string TrayStopRun => T("Stop run", "توقف اجرا");

    public static string TrayQuit => T("Quit", "خروج");

    // --- Run history dialog ------------------------------------------------

    public static string RunHistoryTitle => T("Run history", "تاریخچهٔ اجراها");

    public static string RunHistoryEmpty => T(
        "No runs yet — start one and it will show up here.",
        "هنوز اجرایی ثبت نشده — یکی را شروع کنید تا اینجا نمایش داده شود.");

    public static string ClearHistoryButton => T("Clear history", "پاک‌کردن تاریخچه");

    public static string Close => T("Close", "بستن");

    public static string ClearHistoryMessage => T(
        "Every entry will be removed. This cannot be undone.",
        "همهٔ موارد حذف خواهند شد. این کار قابل بازگشت نیست.");

    public static string ClearConfirm => T("Clear", "پاک کردن");

    public static string IterationsCount(long count) =>
        T($"{count:N0} iteration{(count == 1 ? "" : "s")}", $"{count:N0} تکرار");
}
