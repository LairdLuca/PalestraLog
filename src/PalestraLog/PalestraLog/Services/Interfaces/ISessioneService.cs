using PalestraLog.Models;

namespace PalestraLog.Services.Interfaces;

public interface ISessioneService
{
    Task<Sessione> IniziaSessioneAsync(int giornoId);
    Task FineSessioneAsync(int sessioneId);

    Task<EsercizioSessione> IniziaEsercizioSessioneAsync(int sessioneId, int esercizioId, int eserciziGiornoId, int ordine, int? gruppoEserciziId = null);
    Task<IList<EsercizioSessione>> IniziaSuperSetSessioneAsync(int sessioneId, int gruppoEserciziId, IList<EsercizioGiorno> membri);
    Task<EsercizioSessione?> GetEsercizioSessioneApertaAsync(int sessioneId, int eserciziGiornoId);
    Task<List<EsercizioSessione>> GetEserciziSessioneAsync(int sessioneId);
    Task FineEsercizioSessioneAsync(int esercizioSessioneId, int fatica, string? noteFatica = null);

    Task<int> SalvaSetRecordAsync(SetRecord record);
    Task<List<SetRecord>> GetSetRecordsAsync(int esercizioSessioneId);

    Task<Sessione?> GetSessioneCorrenteAsync();
    Task<List<Sessione>> GetSessioniPerGiornoAsync(int giornoId);
    Task<EsercizioSessione?> GetEsercizioSessioneCorrenteAsync(int sessioneId);

    Task EliminaSessioneAsync(int sessioneId);
}
