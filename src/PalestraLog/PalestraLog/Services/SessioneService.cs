using PalestraLog.Models;
using PalestraLog.Services.Interfaces;

namespace PalestraLog.Services;

public class SessioneService(IDatabaseService db) : ISessioneService
{
    public async Task<Sessione> IniziaSessioneAsync(int giornoId)
    {
        await db.InitializeAsync();
        var sessione = new Sessione
        {
            GiornoId = giornoId,
            DataInizio = DateTime.Now
        };
        await db.Database.InsertAsync(sessione);
        return sessione;
    }

    public async Task FineSessioneAsync(int sessioneId)
    {
        await db.InitializeAsync();
        var sessione = await db.Database.Table<Sessione>()
            .Where(s => s.Id == sessioneId)
            .FirstOrDefaultAsync();

        if (sessione is null) return;

        sessione.DataFine = DateTime.Now;
        sessione.DurataTotaleSecondi = (int)(sessione.DataFine.Value - sessione.DataInizio).TotalSeconds;
        await db.Database.UpdateAsync(sessione);
    }

    public async Task<EsercizioSessione> IniziaEsercizioSessioneAsync(int sessioneId, int esercizioId, int eserciziGiornoId, int ordine, int? gruppoEserciziId = null)
    {
        await db.InitializeAsync();
        var es = new EsercizioSessione
        {
            SessioneId = sessioneId,
            EsercizioId = esercizioId,
            EserciziGiornoId = eserciziGiornoId,
            GruppoEserciziId = gruppoEserciziId,
            Ordine = ordine,
            DataInizio = DateTime.Now
        };
        await db.Database.InsertAsync(es);
        return es;
    }

    public async Task<IList<EsercizioSessione>> IniziaSuperSetSessioneAsync(int sessioneId, int gruppoEserciziId, IList<EsercizioGiorno> membri)
    {
        await db.InitializeAsync();
        var now = DateTime.Now;
        var creati = membri.Select(eg => new EsercizioSessione
        {
            SessioneId = sessioneId,
            EsercizioId = eg.EsercizioId,
            EserciziGiornoId = eg.Id,
            GruppoEserciziId = gruppoEserciziId,
            Ordine = eg.Ordine,
            DataInizio = now
        }).ToList();

        await db.Database.RunInTransactionAsync(tran =>
        {
            foreach (var es in creati)
                tran.Insert(es);
        });

        return creati;
    }

    public async Task<EsercizioSessione?> GetEsercizioSessioneApertaAsync(int sessioneId, int eserciziGiornoId)
    {
        await db.InitializeAsync();
        return await db.Database.Table<EsercizioSessione>()
            .Where(es => es.SessioneId == sessioneId
                      && es.EserciziGiornoId == eserciziGiornoId
                      && es.DataFine == null)
            .OrderByDescending(es => es.DataInizio)
            .FirstOrDefaultAsync();
    }

    public async Task<List<EsercizioSessione>> GetEserciziSessioneAsync(int sessioneId)
    {
        await db.InitializeAsync();
        return await db.Database.Table<EsercizioSessione>()
            .Where(es => es.SessioneId == sessioneId)
            .OrderBy(es => es.Ordine)
            .ThenBy(es => es.DataInizio)
            .ToListAsync();
    }

    public async Task FineEsercizioSessioneAsync(int esercizioSessioneId, int fatica, string? noteFatica = null)
    {
        await db.InitializeAsync();
        var es = await db.Database.Table<EsercizioSessione>()
            .Where(e => e.Id == esercizioSessioneId)
            .FirstOrDefaultAsync();

        if (es is null) return;

        es.DataFine = DateTime.Now;
        es.TempoTotaleSecondi = (int)(es.DataFine.Value - es.DataInizio).TotalSeconds;
        es.Fatica = fatica;
        es.NoteFatica = noteFatica;
        await db.Database.UpdateAsync(es);
    }

    public async Task<int> SalvaSetRecordAsync(SetRecord record)
    {
        await db.InitializeAsync();
        if (record.Id != 0)
        {
            await db.Database.UpdateAsync(record);
            return record.Id;
        }
        await db.Database.InsertAsync(record);
        return record.Id;
    }

    public async Task<List<SetRecord>> GetSetRecordsAsync(int esercizioSessioneId)
    {
        await db.InitializeAsync();
        return await db.Database.Table<SetRecord>()
            .Where(sr => sr.EsercizioSessioneId == esercizioSessioneId)
            .OrderBy(sr => sr.NumeroSet)
            .ToListAsync();
    }

    public async Task<Sessione?> GetSessioneCorrenteAsync()
    {
        await db.InitializeAsync();
        return await db.Database.Table<Sessione>()
            .Where(s => s.DataFine == null)
            .OrderByDescending(s => s.DataInizio)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Sessione>> GetSessioniPerGiornoAsync(int giornoId)
    {
        await db.InitializeAsync();
        return await db.Database.Table<Sessione>()
            .Where(s => s.GiornoId == giornoId)
            .OrderByDescending(s => s.DataInizio)
            .ToListAsync();
    }

    public async Task<EsercizioSessione?> GetEsercizioSessioneCorrenteAsync(int sessioneId)
    {
        await db.InitializeAsync();
        return await db.Database.Table<EsercizioSessione>()
            .Where(es => es.SessioneId == sessioneId && es.DataFine == null)
            .FirstOrDefaultAsync();
    }

    public async Task EliminaSessioneAsync(int sessioneId)
    {
        await db.InitializeAsync();

        var esercizi = await db.Database.Table<EsercizioSessione>()
            .Where(es => es.SessioneId == sessioneId)
            .ToListAsync();

        var esercizioSessioneIds = esercizi.Select(e => e.Id).ToList();

        await db.Database.RunInTransactionAsync(tran =>
        {
            foreach (var esId in esercizioSessioneIds)
                tran.Execute("DELETE FROM SetRecords WHERE EsercizioSessioneId = ?", esId);

            tran.Execute("DELETE FROM EserciziSessione WHERE SessioneId = ?", sessioneId);
            tran.Execute("DELETE FROM Sessioni WHERE Id = ?", sessioneId);
        });
    }
}
