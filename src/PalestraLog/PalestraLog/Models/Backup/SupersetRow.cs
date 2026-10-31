namespace PalestraLog.Models.Backup;

/// <summary>
/// Super set. Origine: tabella GruppiEsercizi. Un gruppo creato al volo durante una
/// sessione è fuori piano: resta per lo storico e nessun esercizio programmato lo riferisce.
/// </summary>
public sealed class SupersetRow
{
    public Guid Uid { get; init; }
    public Guid RoutineUid { get; init; }
    public string? Name { get; init; }
    public int Position { get; init; }
    public bool IsOffPlan { get; init; }
}
