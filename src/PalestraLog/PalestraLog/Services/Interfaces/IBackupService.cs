using PalestraLog.Models.Backup;

namespace PalestraLog.Services.Interfaces;

public interface IBackupService
{
    /// <summary>Legge tutte le tabelle in un'unica transazione e le traduce nel modello di destinazione.</summary>
    Task<BackupFile> ComposeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Compone il file dei dati e lo scrive su <paramref name="destination"/>, senza chiuderlo.
    /// Se fallisce o viene annullato a metà, nel flusso può restare JSON troncato: il chiamante
    /// scarta il file intero e dichiara che il backup non esiste.
    /// </summary>
    Task WriteDataFileAsync(Stream destination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scrive in <paramref name="destinationPath"/> una copia coerente del database, che si
    /// ripristina rimettendola al posto dell'originale. Il file di destinazione non deve esistere.
    /// </summary>
    Task WriteSnapshotAsync(string destinationPath);
}
