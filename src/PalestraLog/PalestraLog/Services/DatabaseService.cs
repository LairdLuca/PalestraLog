using PalestraLog.Helpers;
using PalestraLog.Models;
using PalestraLog.Services.Interfaces;
using SQLite;

namespace PalestraLog.Services;

public class DatabaseService : IDatabaseService
{
    private SQLiteAsyncConnection? _database;

    public SQLiteAsyncConnection Database =>
        _database ?? throw new InvalidOperationException("Database non inizializzato. Chiamare InitializeAsync() prima.");

    public async Task InitializeAsync()
    {
        if (_database is not null)
            return;

        _database = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);

        await _database.CreateTableAsync<Scheda>();
        await _database.CreateTableAsync<Giorno>();
        await _database.CreateTableAsync<Esercizio>();
        await _database.CreateTableAsync<EsercizioGiorno>();
        await _database.CreateTableAsync<GruppoEsercizi>();
        await _database.CreateTableAsync<Sessione>();
        await _database.CreateTableAsync<EsercizioSessione>();
        await _database.CreateTableAsync<SetRecord>();
        await _database.CreateTableAsync<VariazioneVelocita>();
    }
}
