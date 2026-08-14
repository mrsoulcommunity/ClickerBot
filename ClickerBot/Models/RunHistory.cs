using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClickerBot;

/// <summary>One finished, stopped, or failed run, kept for the history list.</summary>
internal sealed class RunHistoryEntry
{
    public string ProfileName { get; set; } = string.Empty;

    /// <summary>How many steps the macro had at the time it ran.</summary>
    public int StepCount { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public TimeSpan Elapsed { get; set; }

    public long Iterations { get; set; }

    /// <summary>The run panel's own closing message — "Finished", "Stopped", or an error.</summary>
    public string Outcome { get; set; } = string.Empty;
}

/// <summary>
/// Loads and saves the run history as JSON under %APPDATA%\ClickerBot\history.json.
///
/// A record of what actually happened while you were away is most of the point of leaving a
/// long run going, so this is written with the same atomic-save discipline as the profiles.
/// </summary>
internal sealed class RunHistoryStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public List<RunHistoryEntry> Entries { get; set; } = new();

    [JsonIgnore]
    private static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClickerBot", "history.json");

    public static RunHistoryStore Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var store = JsonSerializer.Deserialize<RunHistoryStore>(File.ReadAllText(FilePath), SerializerOptions);
                if (store is not null)
                {
                    store.Entries ??= new List<RunHistoryEntry>();
                    store.Entries.RemoveAll(entry => entry is null);
                    return store;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Corrupt or unreadable: history is a convenience, not something worth failing
            // startup over. Start fresh.
        }

        return new RunHistoryStore();
    }

    /// <summary>Adds an entry at the front, keeping only the most recent entries.</summary>
    public void Record(RunHistoryEntry entry)
    {
        Entries.Insert(0, entry);
        if (Entries.Count > Limits.MaxHistoryEntries)
        {
            Entries.RemoveRange(Limits.MaxHistoryEntries, Entries.Count - Limits.MaxHistoryEntries);
        }
    }

    public void Clear() => Entries.Clear();

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

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
}
