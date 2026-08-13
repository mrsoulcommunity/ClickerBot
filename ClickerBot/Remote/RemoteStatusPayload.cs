namespace ClickerBot;

/// <summary>A JSON-serializable snapshot of a run, for the phone page's status poll.</summary>
internal sealed record RemoteStatusPayload(
    bool Running,
    string ProfileName,
    string Mode,
    long Iteration,
    long? Target,
    double ElapsedSeconds,
    string Message);
