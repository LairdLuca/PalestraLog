using SQLite;

namespace PalestraLog.Models;

[Table("GruppiEsercizi")]
public class GruppoEsercizi
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int GiornoId { get; set; }

    public int Ordine { get; set; }

    [MaxLength(100)]
    public string? Nome { get; set; }

    /// <summary>
    /// Gruppo creato al volo durante la sessione e non destinato a persistere nella scheda.
    /// Viene auto-archiviato a FineSessione (i membri perdono il GruppoId, il record resta
    /// con IsArchiviato=true così lo storico continua a vederlo come super set).
    /// </summary>
    public bool IsTemporaneo { get; set; }

    /// <summary>
    /// Gruppo non più visibile nella lista esercizi del giorno (l'esecuzione è terminata).
    /// I record EsercizioSessione che lo referenziano via GruppoEserciziId restano per lo storico.
    /// </summary>
    public bool IsArchiviato { get; set; }
}
