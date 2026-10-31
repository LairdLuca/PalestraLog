using SQLite;

namespace PalestraLog.Services.Interfaces;

public interface IDatabaseService
{
    SQLiteAsyncConnection Database { get; }
    Task InitializeAsync();
}
