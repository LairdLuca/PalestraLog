using SQLite;

namespace PalestraLog.Models;

[Table("Schede")]
public class Scheda
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull, MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descrizione { get; set; }

    public DateTime DataCreazione { get; set; } = DateTime.Now;

    public bool IsAttiva { get; set; }

    public int Ordine { get; set; }
}
