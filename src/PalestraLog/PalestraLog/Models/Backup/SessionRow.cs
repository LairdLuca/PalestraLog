namespace PalestraLog.Models.Backup;

/// <summary>Sessione. Origine: tabella Sessioni. Una sessione mai conclusa è inclusa e marcata incompleta.</summary>
public sealed class SessionRow
{
    public Guid Uid { get; init; }
    public Guid RoutineUid { get; init; }
    public string? RoutineName { get; init; }
    public Guid? VenueUid { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public int? DurationSeconds { get; init; }
    public bool IsIncomplete { get; init; }
    public string? Notes { get; init; }
    public List<string> NotRecorded { get; init; } = [];
}
