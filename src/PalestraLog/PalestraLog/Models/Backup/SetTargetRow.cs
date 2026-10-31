namespace PalestraLog.Models.Backup;

/// <summary>Riga della sequenza di set prevista. Origine: tabella EserciziGiorno, una riga per serie.</summary>
public sealed class SetTargetRow
{
    public Guid Uid { get; init; }
    public Guid ExerciseTargetUid { get; init; }
    public int Position { get; init; }
    public SetType SetType { get; init; }
    public int? Reps { get; init; }
    public double? Weight { get; init; }
    public int? DurationSeconds { get; init; }
    public int? RestSeconds { get; init; }
    public double? Speed { get; init; }
    public double? Incline { get; init; }
    public double? Distance { get; init; }
    public List<string> NotRecorded { get; init; } = [];
}
