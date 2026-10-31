using PalestraLog.Models;
using PalestraLog.Services.Interfaces;

namespace PalestraLog.Services;

public class StatisticheService(IDatabaseService db) : IStatisticheService
{
    public async Task<StatisticheEsercizio> GetStatisticheAsync(int esercizioId, int eserciziGiornoId)
    {
        await db.InitializeAsync();
        var stats = new StatisticheEsercizio();

        // Record assoluto: peso massimo dell'esercizio in qualsiasi contesto, escluso warmup.
        var record = await db.Database.QueryAsync<RecordQueryResult>(
            """
            SELECT
                MAX(sr.Peso) as PesoMassimo,
                MAX(sr.Ripetizioni) as RipetizioniMassime,
                MAX(sr.DurataCardioSecondi) as DurataCardioMassima
            FROM SetRecords sr
            INNER JOIN EserciziSessione es ON sr.EsercizioSessioneId = es.Id
            WHERE es.EsercizioId = ? AND sr.IsRiscaldamento = 0
            """, esercizioId);

        if (record.Count > 0)
        {
            stats.PesoMassimo = record[0].PesoMassimo;
            stats.RipetizioniMassime = record[0].RipetizioniMassime;
            stats.DurataCardioMassima = record[0].DurataCardioMassima;
        }

        // Numero settimana = quante volte questo eserciziGiorno è già stato eseguito (sessioni completate) + 1.
        var occorrenze = await db.Database.QueryAsync<CountQueryResult>(
            """
            SELECT COUNT(DISTINCT s.Id) as Conteggio
            FROM Sessioni s
            INNER JOIN EserciziSessione es ON es.SessioneId = s.Id
            WHERE es.EserciziGiornoId = ? AND s.DataFine IS NOT NULL
            """, eserciziGiornoId);

        stats.NumeroSettimana = (occorrenze.Count > 0 ? occorrenze[0].Conteggio : 0) + 1;

        // Ultima volta in QUESTO eserciziGiorno: primo set non-warmup dell'EsercizioSessione più recente completata.
        var ultimaVolta = await db.Database.QueryAsync<UltimaVoltaQueryResult>(
            """
            SELECT
                sr.Peso, sr.Ripetizioni, sr.DurataSecondi, sr.DurataCardioSecondi,
                sr.Velocita, sr.Inclinazione, sr.Distanza,
                es.Fatica
            FROM SetRecords sr
            INNER JOIN EserciziSessione es ON sr.EsercizioSessioneId = es.Id
            WHERE es.EserciziGiornoId = ?
              AND es.DataFine IS NOT NULL
              AND sr.IsRiscaldamento = 0
            ORDER BY es.DataInizio DESC, sr.NumeroSet ASC
            LIMIT 1
            """, eserciziGiornoId);

        if (ultimaVolta.Count > 0)
        {
            var u = ultimaVolta[0];
            stats.UltimaVoltaPeso = u.Peso;
            stats.UltimaVoltaReps = u.Ripetizioni;
            stats.UltimaVoltaDurataSec = u.DurataSecondi;
            stats.UltimaVoltaDurataCardioSec = u.DurataCardioSecondi;
            stats.UltimaVoltaVelocita = u.Velocita;
            stats.UltimaVoltaInclinazione = u.Inclinazione;
            stats.UltimaVoltaDistanza = u.Distanza;
            stats.UltimaVoltaFatica = u.Fatica > 0 ? u.Fatica : null;
        }

        // Sets dell'ultima sessione (per Storico, non per HUD).
        stats.UltimaSessioneSets = await GetUltimaSessioneSetsAsync(esercizioId);

        // Dati scheda (target + note) per questo eserciziGiorno specifico.
        stats.DatiScheda = await db.Database.Table<EsercizioGiorno>()
            .Where(eg => eg.Id == eserciziGiornoId)
            .FirstOrDefaultAsync();

        return stats;
    }

    public async Task<List<SetRecord>> GetUltimaSessioneSetsAsync(int esercizioId)
    {
        await db.InitializeAsync();
        return await db.Database.QueryAsync<SetRecord>(
            """
            SELECT sr.*
            FROM SetRecords sr
            INNER JOIN EserciziSessione es ON sr.EsercizioSessioneId = es.Id
            WHERE es.EsercizioId = ?
            AND es.DataFine IS NOT NULL
            ORDER BY es.DataInizio DESC, sr.NumeroSet ASC
            LIMIT 20
            """, esercizioId);
    }

    private class RecordQueryResult
    {
        public double PesoMassimo { get; set; }
        public int RipetizioniMassime { get; set; }
        public int DurataCardioMassima { get; set; }
    }

    private class CountQueryResult
    {
        public int Conteggio { get; set; }
    }

    private class UltimaVoltaQueryResult
    {
        public double? Peso { get; set; }
        public int? Ripetizioni { get; set; }
        public int? DurataSecondi { get; set; }
        public int? DurataCardioSecondi { get; set; }
        public double? Velocita { get; set; }
        public double? Inclinazione { get; set; }
        public double? Distanza { get; set; }
        public int Fatica { get; set; }
    }
}
