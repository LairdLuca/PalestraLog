using SQLite;

namespace PalestraLog.Models;

[Table("SetRecords")]
public class SetRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int EsercizioSessioneId { get; set; }

    public int NumeroSet { get; set; }

    public bool IsRiscaldamento { get; set; }

    // Pesi + CorpoLibero
    public double? Peso { get; set; }
    public int? Ripetizioni { get; set; }

    // Tempo del set (dal lap timer)
    public int DurataSecondi { get; set; }

    // Cardio
    public double? Velocita { get; set; }
    public double? Inclinazione { get; set; }
    public int? DurataCardioSecondi { get; set; }
    public double? Distanza { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.Now;
}
