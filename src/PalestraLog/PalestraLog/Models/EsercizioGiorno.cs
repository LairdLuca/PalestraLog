using SQLite;

namespace PalestraLog.Models;

/// <summary>
/// Tabella ponte molti-a-molti tra Esercizi e Giorni.
/// Contiene i target specifici per la combinazione esercizio+giorno.
/// </summary>
[Table("EserciziGiorno")]
public class EsercizioGiorno
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int EsercizioId { get; set; }

    [Indexed]
    public int GiornoId { get; set; }

    public int Ordine { get; set; }

    [Indexed]
    public int? GruppoId { get; set; }

    public bool IsTemporaneo { get; set; }

    // Target comuni
    public int? SerieTarget { get; set; }
    public int? RipetizioniTarget { get; set; }
    public double? PesoTarget { get; set; }
    public int? DurataTargetSecondi { get; set; }

    // Target cardio
    public double? VelocitaTarget { get; set; }
    public double? InclinazioneTarget { get; set; }
    public double? DistanzaTarget { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}
