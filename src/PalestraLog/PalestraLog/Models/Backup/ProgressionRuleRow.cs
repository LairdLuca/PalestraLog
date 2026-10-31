namespace PalestraLog.Models.Backup;

/// <summary>
/// Regola di progressione. La app precedente non ne ha: nella versione 1 del formato la
/// collezione è sempre vuota, e i campi della regola nasceranno con la funzione.
/// </summary>
public sealed class ProgressionRuleRow
{
    public Guid Uid { get; init; }
    public Guid RoutineExerciseUid { get; init; }
}
