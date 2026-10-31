namespace PalestraLog.Models.Backup;

/// <summary>Scheda. Origine: tabella Schede.</summary>
public sealed class ProgramRow
{
    public Guid Uid { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public int Position { get; init; }
    public DateTime CreatedAt { get; init; }
}
