namespace PalestraLog.Models.Backup;

/// <summary>Routine (era «giorno»). Origine: tabella Giorni.</summary>
public sealed class RoutineRow
{
    public Guid Uid { get; init; }
    public Guid ProgramUid { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Position { get; init; }
}
