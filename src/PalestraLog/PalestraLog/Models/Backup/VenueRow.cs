namespace PalestraLog.Models.Backup;

/// <summary>
/// Luogo di allenamento. La app precedente non ne ha: nella versione 1 del formato la
/// collezione è sempre vuota, e i campi del luogo nasceranno con la funzione.
/// </summary>
public sealed class VenueRow
{
    public Guid Uid { get; init; }
    public string Name { get; init; } = string.Empty;
}
