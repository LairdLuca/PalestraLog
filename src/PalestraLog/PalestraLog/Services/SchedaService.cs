using PalestraLog.Models;
using PalestraLog.Services.Interfaces;
using SQLite;

namespace PalestraLog.Services;

public class SchedaService(IDatabaseService db) : ISchedaService
{
    // --- Schede ---

    public async Task<List<Scheda>> GetSchedeAsync()
    {
        await db.InitializeAsync();
        return await db.Database.Table<Scheda>().OrderBy(s => s.Ordine).ToListAsync();
    }

    public async Task<Scheda?> GetSchedaAttivaAsync()
    {
        await db.InitializeAsync();
        return await db.Database.Table<Scheda>().Where(s => s.IsAttiva).FirstOrDefaultAsync();
    }

    public async Task<Scheda?> GetSchedaByIdAsync(int id)
    {
        await db.InitializeAsync();
        return await db.Database.Table<Scheda>().Where(s => s.Id == id).FirstOrDefaultAsync();
    }

    public async Task SetSchedaAttivaAsync(int schedaId)
    {
        await db.InitializeAsync();
        // Disattiva tutte
        await db.Database.ExecuteAsync("UPDATE Schede SET IsAttiva = 0");
        // Attiva quella selezionata
        await db.Database.ExecuteAsync("UPDATE Schede SET IsAttiva = 1 WHERE Id = ?", schedaId);
    }

    public async Task<int> SaveSchedaAsync(Scheda scheda)
    {
        await db.InitializeAsync();
        if (scheda.Id != 0)
        {
            await db.Database.UpdateAsync(scheda);
            return scheda.Id;
        }
        await db.Database.InsertAsync(scheda);
        return scheda.Id;
    }

    public async Task DeleteSchedaAsync(int id)
    {
        await db.InitializeAsync();
        // Elimina a cascata: giorni, esercizi-giorno, sessioni
        var giorni = await db.Database.Table<Giorno>().Where(g => g.SchedaId == id).ToListAsync();
        foreach (var giorno in giorni)
        {
            await DeleteGiornoAsync(giorno.Id);
        }
        await db.Database.DeleteAsync<Scheda>(id);
    }

    // --- Giorni ---

    public async Task<List<Giorno>> GetGiorniAsync(int schedaId)
    {
        await db.InitializeAsync();
        return await db.Database.Table<Giorno>()
            .Where(g => g.SchedaId == schedaId)
            .OrderBy(g => g.Ordine)
            .ToListAsync();
    }

    public async Task<int> SaveGiornoAsync(Giorno giorno)
    {
        await db.InitializeAsync();
        if (giorno.Id != 0)
        {
            await db.Database.UpdateAsync(giorno);
            return giorno.Id;
        }
        await db.Database.InsertAsync(giorno);
        return giorno.Id;
    }

    public async Task DeleteGiornoAsync(int id)
    {
        await db.InitializeAsync();
        await db.Database.RunInTransactionAsync(tran =>
        {
            tran.Execute("DELETE FROM EserciziGiorno WHERE GiornoId = ?", id);
            tran.Execute("DELETE FROM GruppiEsercizi WHERE GiornoId = ?", id);
            tran.Delete<Giorno>(id);
        });
    }

    // --- Esercizi (catalogo) ---

    public async Task<List<Esercizio>> GetEserciziCatalogoAsync()
    {
        await db.InitializeAsync();
        return await db.Database.Table<Esercizio>().OrderBy(e => e.Nome).ToListAsync();
    }

    public async Task<Esercizio?> GetEsercizioAsync(int id)
    {
        await db.InitializeAsync();
        return await db.Database.Table<Esercizio>().Where(e => e.Id == id).FirstOrDefaultAsync();
    }

    public async Task<int> SaveEsercizioAsync(Esercizio esercizio)
    {
        await db.InitializeAsync();
        if (esercizio.Id != 0)
        {
            await db.Database.UpdateAsync(esercizio);
            return esercizio.Id;
        }
        await db.Database.InsertAsync(esercizio);
        return esercizio.Id;
    }

    public async Task DeleteEsercizioAsync(int id)
    {
        await db.InitializeAsync();
        await db.Database.ExecuteAsync("DELETE FROM EserciziGiorno WHERE EsercizioId = ?", id);
        await db.Database.DeleteAsync<Esercizio>(id);
    }

    // --- EserciziGiorno (ponte) ---

    public async Task<List<EsercizioGiorno>> GetEserciziGiornoAsync(int giornoId)
    {
        await db.InitializeAsync();
        return await db.Database.Table<EsercizioGiorno>()
            .Where(eg => eg.GiornoId == giornoId)
            .OrderBy(eg => eg.Ordine)
            .ToListAsync();
    }

    public async Task<EsercizioGiorno?> GetEsercizioGiornoAsync(int esercizioId, int giornoId)
    {
        await db.InitializeAsync();
        return await db.Database.Table<EsercizioGiorno>()
            .Where(eg => eg.EsercizioId == esercizioId && eg.GiornoId == giornoId)
            .FirstOrDefaultAsync();
    }

    public async Task<EsercizioGiorno?> GetEsercizioGiornoByIdAsync(int id)
    {
        await db.InitializeAsync();
        return await db.Database.Table<EsercizioGiorno>()
            .Where(eg => eg.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<int> SaveEsercizioGiornoAsync(EsercizioGiorno eg)
    {
        await db.InitializeAsync();
        if (eg.Id != 0)
        {
            await db.Database.UpdateAsync(eg);
            return eg.Id;
        }
        await db.Database.InsertAsync(eg);
        return eg.Id;
    }

    public async Task DeleteEsercizioGiornoAsync(int id)
    {
        await db.InitializeAsync();
        var eg = await db.Database.Table<EsercizioGiorno>().Where(x => x.Id == id).FirstOrDefaultAsync();
        if (eg is null) return;

        await db.Database.RunInTransactionAsync(tran =>
        {
            tran.Delete<EsercizioGiorno>(id);
            // Se l'esercizio era in un gruppo, eventualmente auto-sciogli
            if (eg.GruppoId is int gruppoId)
                AutoSciogliSeRimastoSolo(tran, gruppoId);
        });
    }

    // --- Gruppi (super set / tri-set / giant set) ---

    public async Task<List<GruppoEsercizi>> GetGruppiGiornoAsync(int giornoId)
    {
        await db.InitializeAsync();
        return await db.Database.Table<GruppoEsercizi>()
            .Where(g => g.GiornoId == giornoId && !g.IsArchiviato)
            .OrderBy(g => g.Ordine)
            .ToListAsync();
    }

    public async Task<int> CreaGruppoAsync(int giornoId, IList<int> esercizioGiornoIds, string? nome = null, int? serieTargetNormalizzato = null, bool isTemporaneo = false)
    {
        if (esercizioGiornoIds.Count < 2)
            throw new ArgumentException("Un super set deve avere almeno 2 esercizi", nameof(esercizioGiornoIds));

        await db.InitializeAsync();

        int newGruppoId = 0;
        var idSet = esercizioGiornoIds.ToHashSet();

        await db.Database.RunInTransactionAsync(tran =>
        {
            // Ordine del nuovo gruppo = max + 1 dei gruppi esistenti del giorno
            var gruppiEsistenti = tran.Table<GruppoEsercizi>()
                .Where(g => g.GiornoId == giornoId)
                .ToList();
            int prossimoOrdineGruppo = gruppiEsistenti.Count > 0 ? gruppiEsistenti.Max(g => g.Ordine) + 1 : 0;

            var gruppo = new GruppoEsercizi
            {
                GiornoId = giornoId,
                Ordine = prossimoOrdineGruppo,
                Nome = nome,
                IsTemporaneo = isTemporaneo
            };
            tran.Insert(gruppo);
            newGruppoId = gruppo.Id;

            // Imposta GruppoId (e SerieTarget se normalizzato) sui membri
            var tuttiEG = tran.Table<EsercizioGiorno>()
                .Where(eg => eg.GiornoId == giornoId)
                .OrderBy(eg => eg.Ordine)
                .ToList();

            foreach (var eg in tuttiEG.Where(e => idSet.Contains(e.Id)))
            {
                eg.GruppoId = newGruppoId;
                if (serieTargetNormalizzato.HasValue)
                    eg.SerieTarget = serieTargetNormalizzato;
                tran.Update(eg);
            }

            // Riarrangia Ordine: i membri del gruppo devono essere consecutivi
            CompattaOrdineDelGruppo(tran, tuttiEG, newGruppoId);
        });

        return newGruppoId;
    }

    public async Task SciogliGruppoAsync(int gruppoId)
    {
        await db.InitializeAsync();
        await db.Database.RunInTransactionAsync(tran =>
        {
            tran.Execute("UPDATE EserciziGiorno SET GruppoId = NULL WHERE GruppoId = ?", gruppoId);
            tran.Delete<GruppoEsercizi>(gruppoId);
        });
    }

    public async Task AggiungiEsercizioAGruppoAsync(int gruppoId, int esercizioGiornoId, int? serieTargetNormalizzato = null)
    {
        await db.InitializeAsync();

        var gruppo = await db.Database.Table<GruppoEsercizi>().Where(g => g.Id == gruppoId).FirstOrDefaultAsync();
        if (gruppo is null) return;

        var esercizio = await db.Database.Table<EsercizioGiorno>().Where(eg => eg.Id == esercizioGiornoId).FirstOrDefaultAsync();
        if (esercizio is null || esercizio.GiornoId != gruppo.GiornoId) return;

        await db.Database.RunInTransactionAsync(tran =>
        {
            esercizio.GruppoId = gruppoId;
            if (serieTargetNormalizzato.HasValue)
                esercizio.SerieTarget = serieTargetNormalizzato;
            tran.Update(esercizio);

            // Propaga SerieTarget anche ai membri esistenti se richiesto
            if (serieTargetNormalizzato.HasValue)
            {
                var altriMembri = tran.Table<EsercizioGiorno>()
                    .Where(eg => eg.GruppoId == gruppoId && eg.Id != esercizio.Id)
                    .ToList();
                foreach (var m in altriMembri)
                {
                    m.SerieTarget = serieTargetNormalizzato;
                    tran.Update(m);
                }
            }

            var tuttiEG = tran.Table<EsercizioGiorno>()
                .Where(eg => eg.GiornoId == gruppo.GiornoId)
                .OrderBy(eg => eg.Ordine)
                .ToList();

            CompattaOrdineDelGruppo(tran, tuttiEG, gruppoId);
        });
    }

    public async Task RimuoviEsercizioDaGruppoAsync(int esercizioGiornoId)
    {
        await db.InitializeAsync();

        var esercizio = await db.Database.Table<EsercizioGiorno>().Where(eg => eg.Id == esercizioGiornoId).FirstOrDefaultAsync();
        if (esercizio is null || esercizio.GruppoId is null) return;

        int gruppoId = esercizio.GruppoId.Value;

        await db.Database.RunInTransactionAsync(tran =>
        {
            esercizio.GruppoId = null;
            tran.Update(esercizio);
            AutoSciogliSeRimastoSolo(tran, gruppoId);
        });
    }

    public async Task RinominaGruppoAsync(int gruppoId, string? nuovoNome)
    {
        await db.InitializeAsync();
        var gruppo = await db.Database.Table<GruppoEsercizi>().Where(g => g.Id == gruppoId).FirstOrDefaultAsync();
        if (gruppo is null) return;
        gruppo.Nome = nuovoNome;
        await db.Database.UpdateAsync(gruppo);
    }

    public async Task ArchiviaGruppiTemporaneiAsync(int giornoId)
    {
        await db.InitializeAsync();
        await db.Database.RunInTransactionAsync(tran =>
        {
            var gruppiTemp = tran.Table<GruppoEsercizi>()
                .Where(g => g.GiornoId == giornoId && g.IsTemporaneo && !g.IsArchiviato)
                .ToList();

            foreach (var g in gruppiTemp)
            {
                tran.Execute("UPDATE EserciziGiorno SET GruppoId = NULL WHERE GruppoId = ?", g.Id);
                g.IsArchiviato = true;
                tran.Update(g);
            }
        });
    }

    /// <summary>
    /// Riassegna `Ordine` a tutti gli `EsercizioGiorno` del giorno garantendo che
    /// i membri del gruppo target siano consecutivi (mantengono il loro ordine relativo
    /// originale e occupano la posizione del primo membro nella sequenza corrente).
    /// </summary>
    private static void CompattaOrdineDelGruppo(SQLiteConnection tran, List<EsercizioGiorno> tuttiEGOrdinati, int gruppoId)
    {
        var membri = tuttiEGOrdinati.Where(eg => eg.GruppoId == gruppoId).ToList();
        if (membri.Count == 0) return;

        var nuovaSequenza = new List<EsercizioGiorno>(tuttiEGOrdinati.Count);
        bool inseritiMembri = false;

        foreach (var eg in tuttiEGOrdinati)
        {
            if (eg.GruppoId == gruppoId)
            {
                if (!inseritiMembri)
                {
                    nuovaSequenza.AddRange(membri);
                    inseritiMembri = true;
                }
                // I successivi membri vengono saltati (già inseriti in blocco)
            }
            else
            {
                nuovaSequenza.Add(eg);
            }
        }

        for (int i = 0; i < nuovaSequenza.Count; i++)
        {
            if (nuovaSequenza[i].Ordine != i)
            {
                nuovaSequenza[i].Ordine = i;
                tran.Update(nuovaSequenza[i]);
            }
        }
    }

    /// <summary>
    /// Se nel gruppo restano meno di 2 membri, scioglie il gruppo: l'eventuale
    /// ultimo membro torna esercizio singolo e il record `GruppoEsercizi` viene eliminato.
    /// Da chiamare dentro a una transazione.
    /// </summary>
    private static void AutoSciogliSeRimastoSolo(SQLiteConnection tran, int gruppoId)
    {
        var membriRimasti = tran.Table<EsercizioGiorno>()
            .Where(eg => eg.GruppoId == gruppoId)
            .ToList();

        if (membriRimasti.Count >= 2) return;

        foreach (var m in membriRimasti)
        {
            m.GruppoId = null;
            tran.Update(m);
        }
        tran.Delete<GruppoEsercizi>(gruppoId);
    }
}
