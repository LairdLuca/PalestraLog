using PalestraLog.Models;

namespace PalestraLog.Services.Interfaces;

public interface ISchedaService
{
    // Schede
    Task<List<Scheda>> GetSchedeAsync();
    Task<Scheda?> GetSchedaAttivaAsync();
    Task<Scheda?> GetSchedaByIdAsync(int id);
    Task SetSchedaAttivaAsync(int schedaId);
    Task<int> SaveSchedaAsync(Scheda scheda);
    Task DeleteSchedaAsync(int id);

    // Giorni
    Task<List<Giorno>> GetGiorniAsync(int schedaId);
    Task<int> SaveGiornoAsync(Giorno giorno);
    Task DeleteGiornoAsync(int id);

    // Esercizi
    Task<List<Esercizio>> GetEserciziCatalogoAsync();
    Task<Esercizio?> GetEsercizioAsync(int id);
    Task<int> SaveEsercizioAsync(Esercizio esercizio);
    Task DeleteEsercizioAsync(int id);

    // EserciziGiorno (ponte)
    Task<List<EsercizioGiorno>> GetEserciziGiornoAsync(int giornoId);
    Task<EsercizioGiorno?> GetEsercizioGiornoAsync(int esercizioId, int giornoId);
    Task<EsercizioGiorno?> GetEsercizioGiornoByIdAsync(int id);
    Task<int> SaveEsercizioGiornoAsync(EsercizioGiorno eg);
    Task DeleteEsercizioGiornoAsync(int id);

    // Gruppi (super set / tri-set / giant set)
    Task<List<GruppoEsercizi>> GetGruppiGiornoAsync(int giornoId);
    Task<int> CreaGruppoAsync(int giornoId, IList<int> esercizioGiornoIds, string? nome = null, int? serieTargetNormalizzato = null, bool isTemporaneo = false);
    Task SciogliGruppoAsync(int gruppoId);
    Task AggiungiEsercizioAGruppoAsync(int gruppoId, int esercizioGiornoId, int? serieTargetNormalizzato = null);
    Task RimuoviEsercizioDaGruppoAsync(int esercizioGiornoId);
    Task RinominaGruppoAsync(int gruppoId, string? nuovoNome);
    Task ArchiviaGruppiTemporaneiAsync(int giornoId);
}
