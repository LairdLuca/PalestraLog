namespace PalestraLog.Models.Backup;

/// <summary>
/// Esercizio programmato. Origine: tabella EserciziGiorno, esclusi gli esercizi
/// temporanei, che nel modello di destinazione sono esecuzioni fuori piano.
/// </summary>
public sealed class RoutineExerciseRow
{
    public Guid Uid { get; init; }
    public Guid RoutineUid { get; init; }
    public Guid ExerciseUid { get; init; }
    public Guid? SupersetUid { get; init; }
    public int Position { get; init; }
    public string? Notes { get; init; }
}
