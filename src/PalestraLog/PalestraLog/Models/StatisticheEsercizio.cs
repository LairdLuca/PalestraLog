namespace PalestraLog.Models;

/// <summary>
/// DTO per le statistiche aggregate di un esercizio.
/// Non è una tabella SQLite, viene popolato dalle query.
/// </summary>
public class StatisticheEsercizio
{
    public int NumeroSettimana { get; set; }

    public double PesoMassimo { get; set; }
    public int RipetizioniMassime { get; set; }
    public int DurataCardioMassima { get; set; }

    public double? UltimaVoltaPeso { get; set; }
    public int? UltimaVoltaReps { get; set; }
    public int? UltimaVoltaDurataSec { get; set; }
    public int? UltimaVoltaDurataCardioSec { get; set; }
    public double? UltimaVoltaVelocita { get; set; }
    public double? UltimaVoltaInclinazione { get; set; }
    public double? UltimaVoltaDistanza { get; set; }
    public int? UltimaVoltaFatica { get; set; }

    public List<SetRecord> UltimaSessioneSets { get; set; } = [];
    public EsercizioGiorno? DatiScheda { get; set; }
}
