namespace PalestraLog.Models.Backup;

/// <summary>Set registrato. Origine: tabella SetRecords.</summary>
public sealed class SetEntryRow
{
    public Guid Uid { get; init; }
    public Guid SessionExerciseUid { get; init; }
    public Guid? SetTargetUid { get; init; }
    public SetType SetType { get; init; }
    public double? Weight { get; init; }
    public int? Reps { get; init; }
    public int? DurationSeconds { get; init; }
    public double? Speed { get; init; }
    public double? Incline { get; init; }
    public double? Distance { get; init; }

    /// <summary>Tempo dal set precedente, riposo compreso. Non è la durata del set.</summary>
    public int SincePreviousSetSeconds { get; init; }

    public DateTime RecordedAt { get; init; }
    public string? Notes { get; init; }
    public List<string> NotRecorded { get; init; } = [];
}
