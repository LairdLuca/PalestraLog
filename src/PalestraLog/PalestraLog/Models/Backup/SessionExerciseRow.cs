namespace PalestraLog.Models.Backup;

/// <summary>Esercizio della sessione. Origine: tabella EserciziSessione.</summary>
public sealed class SessionExerciseRow
{
    public Guid Uid { get; init; }
    public Guid SessionUid { get; init; }
    public Guid ExerciseUid { get; init; }
    public Guid? RoutineExerciseUid { get; init; }
    public Guid? SupersetUid { get; init; }
    public int Position { get; init; }
    public SessionExerciseStatus Status { get; init; }
    public bool? IsOffPlan { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public int? DurationSeconds { get; init; }
    public int? Effort { get; init; }
    public string? EffortNotes { get; init; }
    public string? ExerciseName { get; init; }
    public int? ExecutionNumber { get; init; }
    public List<string> NotRecorded { get; init; } = [];
}
