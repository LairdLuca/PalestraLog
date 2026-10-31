namespace PalestraLog.Models.Backup;

/// <summary>
/// Abbonamento. La app precedente non ne ha: nella versione 1 del formato la collezione è
/// sempre vuota, e i campi dell'abbonamento nasceranno con la funzione.
/// </summary>
public sealed class MembershipRow
{
    public Guid Uid { get; init; }
    public Guid VenueUid { get; init; }
}
