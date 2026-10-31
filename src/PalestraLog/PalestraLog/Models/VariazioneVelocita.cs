using SQLite;

namespace PalestraLog.Models;

[Table("VariazioniVelocita")]
public class VariazioneVelocita
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int SetRecordId { get; set; }

    public int SecondiDallInizio { get; set; }

    public double Velocita { get; set; }

    public double Inclinazione { get; set; }
}
