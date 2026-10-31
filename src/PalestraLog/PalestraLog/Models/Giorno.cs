using SQLite;

namespace PalestraLog.Models;

[Table("Giorni")]
public class Giorno
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int SchedaId { get; set; }

    [NotNull, MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    public int Ordine { get; set; }
}
