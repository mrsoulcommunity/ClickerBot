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
                if (store is { Profiles.Count: > 0 })
                {
                    store.SelectedIndex = Math.Clamp(store.SelectedIndex, 0, store.Profiles.Count - 1);
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

    private static string PathUnder(string folder) => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        folder,
        "profiles.json");

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, SerializerOptions));
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
