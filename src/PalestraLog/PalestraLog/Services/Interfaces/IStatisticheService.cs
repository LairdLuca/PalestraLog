using PalestraLog.Models;

namespace PalestraLog.Services.Interfaces;

public interface IStatisticheService
{
    Task<StatisticheEsercizio> GetStatisticheAsync(int esercizioId, int eserciziGiornoId);
    Task<List<SetRecord>> GetUltimaSessioneSetsAsync(int esercizioId);
}
