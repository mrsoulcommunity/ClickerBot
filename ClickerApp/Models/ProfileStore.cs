using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClickerApp;

/// <summary>
/// Loads and saves the profile collection as JSON under %APPDATA%\ClickerApp\profiles.json.
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

    [JsonIgnore]
    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ClickerApp",
        "profiles.json");

    public static ProfileStore Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var store = JsonSerializer.Deserialize<ProfileStore>(File.ReadAllText(FilePath), SerializerOptions);
                if (store is { Profiles.Count: > 0 })
                {
                    store.SelectedIndex = Math.Clamp(store.SelectedIndex, 0, store.Profiles.Count - 1);
                    return store;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Corrupt or unreadable file: fall through to a fresh default profile.
        }

        return CreateDefault();
    }

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

    private static ProfileStore CreateDefault() => new()
    {
        Profiles = { new Profile { Name = "Default" } },
        SelectedIndex = 0,
    };
}
