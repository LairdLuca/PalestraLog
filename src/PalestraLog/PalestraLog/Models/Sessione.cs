using SQLite;

namespace PalestraLog.Models;

[Table("Sessioni")]
public class Sessione
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int GiornoId { get; set; }

    public DateTime DataInizio { get; set; }

    public DateTime? DataFine { get; set; }

    public int DurataTotaleSecondi { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }
}
