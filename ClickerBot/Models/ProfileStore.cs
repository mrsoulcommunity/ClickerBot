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
    private static ProfileStore CreateDefault() => new()
    {
        Profiles = { new Profile { Name = "Default" } },
        SelectedIndex = 0,
        Appearance = WindowChrome.SystemPreference(),
    };
}
