namespace PalestraLog.Models.Backup;

/// <summary>
/// Intestazione dell'obiettivo. Origine: tabella EserciziGiorno, una per esercizio
/// programmato (AD-23). Non porta valori: stanno nelle righe di sequenza.
/// </summary>
public sealed class ExerciseTargetRow
{
    public Guid Uid { get; init; }
    public Guid RoutineExerciseUid { get; init; }
    public int FromExecution { get; init; }
}
