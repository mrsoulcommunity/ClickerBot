using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClickerBot;

/// <summary>
/// Loads and saves the profile collection as JSON under %APPDATA%\ClickerBot\profiles.json.
/// </summary>
internal sealed class ProfileStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public List<Profile> Profiles { get; set; } = new();

    public int SelectedIndex { get; set; }

    /// <summary>
    /// The chosen appearance. Application-wide rather than per-profile: switching profiles
    /// should never repaint the window. Named "Appearance" so it does not shadow
    /// <see cref="Theme"/> inside this class.
    /// </summary>
    public ThemeMode Appearance { get; set; } = ThemeMode.Light;

    /// <summary>Keeps the window above the one being automated. Application-wide, like the theme.</summary>
    public bool AlwaysOnTop { get; set; }

    /// <summary>Drops the window to the notification area for the duration of a run.</summary>
    public bool HideToTrayWhileRunning { get; set; }

    /// <summary>
    /// Aborts a run the moment the real cursor touches a screen corner — a backstop for when
    /// the configured Stop hotkey cannot be registered. On by default: a safety net should not
    /// need to be opted into.
    /// </summary>
    public bool FailsafeEnabled { get; set; } = true;

    /// <summary>
    /// Whether the phone remote server should be running. Off by default — unlike the
    /// failsafe, this opens a network port, so it is opt-in rather than opt-out.
    /// </summary>
    public bool RemoteControlEnabled { get; set; }

    /// <summary>
    /// The interface language. Application-wide, like the theme. A first run starts on
    /// whatever <see cref="Loc.SystemPreference"/> reads off Windows' own display language.
    /// </summary>
    public Language Language { get; set; } = Loc.SystemPreference();

    [JsonIgnore]
    public static string FilePath { get; } = PathUnder("ClickerBot");

    /// <summary>
    /// Where profiles lived before the app was renamed. Read once, on the first run after
    /// the rename, so an existing setup is not silently replaced by a blank default.
    /// </summary>
    [JsonIgnore]
    private static string LegacyFilePath { get; } = PathUnder("ClickerApp");

    public static ProfileStore Load() => ReadFrom(FilePath) ?? ReadFrom(LegacyFilePath) ?? CreateDefault();

    private static ProfileStore? ReadFrom(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var store = JsonSerializer.Deserialize<ProfileStore>(File.ReadAllText(path), SerializerOptions);
                if (store is not null && store.Sanitize())
                {
                    return store;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Corrupt or unreadable file: treat it as absent.
        }

        return null;
    }

    /// <summary>
    /// Makes a just-deserialized store safe to hand to the UI. JSON can legally contain a
    /// null entry, or values no input would ever produce, and every screen downstream
    /// assumes neither.
    /// </summary>
    /// <returns>False when nothing usable was left, which reads as "no file here".</returns>
    private bool Sanitize()
    {
        // "Profiles": null deserializes to a null list, past the property initializer.
        if (Profiles is null)
        {
            return false;
        }

        Profiles.RemoveAll(profile => profile is null);
        if (Profiles.Count == 0)
        {
            return false;
        }

        foreach (var profile in Profiles)
        {
            profile.Normalize();
        }

        SelectedIndex = Math.Clamp(SelectedIndex, 0, Profiles.Count - 1);
        Appearance = Appearance == ThemeMode.Dark ? ThemeMode.Dark : ThemeMode.Light;
        Language = Language == Language.Persian ? Language.Persian : Language.English;
        return true;
    }

    private static string PathUnder(string folder) => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        folder,
        "profiles.json");

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

        // Written beside the real file and swapped in, because saves happen constantly while
        // typing: a write interrupted half way through would otherwise leave a truncated file
        // where every profile used to be, and that file is the only copy.
        string temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(this, SerializerOptions));

        if (File.Exists(FilePath))
        {
            File.Replace(temporary, FilePath, destinationBackupFileName: null);
        }
        else
        {
            File.Move(temporary, FilePath);
        }
    }

    /// <summary>
    /// Reads just the profiles out of a file written by <see cref="ExportTo"/> — or, since
    /// they are the same shape, out of another machine's profiles.json.
    /// </summary>
    /// <returns>Null when the file is missing, unreadable, or holds nothing usable.</returns>
    public static List<Profile>? ReadProfiles(string path)
    {
        try
        {
            var store = JsonSerializer.Deserialize<ProfileStore>(File.ReadAllText(path), SerializerOptions);
            return store is not null && store.Sanitize() ? store.Profiles : null;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Writes the profiles to <paramref name="path"/>, leaving app settings out of it.</summary>
    public void ExportTo(string path)
    {
        var bundle = new ProfileStore { Profiles = Profiles, SelectedIndex = 0 };
        File.WriteAllText(path, JsonSerializer.Serialize(bundle, SerializerOptions));
    }

    /// <summary>Returns a name that is not yet used, e.g. "Profile 2".</summary>
    public string CreateUniqueName(string baseName)
    {
        if (!Profiles.Any(p => string.Equals(p.Name, baseName, StringComparison.OrdinalIgnoreCase)))
        {
            return baseName;
        }

        for (int i = 2; ; i++)
        {
            string candidate = $"{baseName} {i}";
            if (!Profiles.Any(p => string.Equals(p.Name, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                return candidate;
            }
        }
    }

    /// <summary>A first run starts on whatever appearance Windows itself is set to.</summary>
    private static ProfileStore CreateDefault()
    {
        var profile = new Profile { Name = Loc.DefaultProfileName };
        // Sanitize() is what normally normalizes a profile, but there is no file to sanitize
        // on a first run — without this, the very first thing a new install shows is an empty
        // step list instead of the starting sequence Normalize builds from the defaults.
        profile.Normalize();

        return new()
        {
            Profiles = { profile },
            SelectedIndex = 0,
            Appearance = WindowChrome.SystemPreference(),
        };
    }
}
