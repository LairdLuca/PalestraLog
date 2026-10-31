using PalestraLog.Models.Enums;
using SQLite;

namespace PalestraLog.Models;

[Table("Esercizi")]
public class Esercizio
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull, MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    public int Tipo { get; set; }

    [MaxLength(500)]
    public string? Descrizione { get; set; }

    [Ignore]
    public TipoEsercizio TipoEsercizio
    {
        get => (TipoEsercizio)Tipo;
        set => Tipo = (int)value;
    }
}
