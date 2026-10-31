using PalestraLog.Models.Enums;
using SQLite;

namespace PalestraLog.Models;

[Table("EserciziSessione")]
public class EsercizioSessione
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int SessioneId { get; set; }

    [Indexed]
    public int EsercizioId { get; set; }

    [Indexed]
    public int EserciziGiornoId { get; set; }

    [Indexed]
    public int? GruppoEserciziId { get; set; }

    public int Ordine { get; set; }

    public int TempoTotaleSecondi { get; set; }

    public DateTime DataInizio { get; set; }

    public DateTime? DataFine { get; set; }

    public int Fatica { get; set; }

    [MaxLength(500)]
    public string? NoteFatica { get; set; }

    [Ignore]
    public LivelloFatica LivelloFatica
    {
        get => (LivelloFatica)Fatica;
        set => Fatica = (int)value;
    }
}
