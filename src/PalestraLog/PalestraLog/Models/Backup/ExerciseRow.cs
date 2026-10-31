namespace PalestraLog.Models.Backup;

/// <summary>Esercizio di catalogo. Origine: tabella Esercizi, una riga per riga, senza fusioni per nome.</summary>
public sealed class ExerciseRow
{
    public Guid Uid { get; init; }
    public string Name { get; init; } = string.Empty;
    public ExerciseType Type { get; init; }
    public string? Description { get; init; }
    public double? WeightStep { get; init; }
    public int? RepsStep { get; init; }
    public int? DurationStepSeconds { get; init; }
    public List<string> NotRecorded { get; init; } = [];
}
