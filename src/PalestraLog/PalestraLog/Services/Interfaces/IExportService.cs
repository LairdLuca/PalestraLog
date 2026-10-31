namespace PalestraLog.Services.Interfaces;

/// <summary>La fase in cui un export si è fermato.</summary>
public enum ExportStage
{
    DataFile,
    Snapshot,
    Delivery
}

/// <summary>
/// Esito di un export. <see cref="HandedToSystem"/> dice soltanto che i due file sono stati
/// affidati al foglio di condivisione: se l'utente abbia poi completato l'invio o annullato,
/// la piattaforma non lo comunica all'app, e l'esito non lo afferma.
/// </summary>
public sealed record ExportResult(bool HandedToSystem, ExportStage? FailedStage = null, string? Error = null);

public interface IExportService
{
    /// <summary>
    /// Produce il file dei dati e la copia del database in una cartella transitoria e, solo se
    /// esistono entrambi, li consegna insieme in un'unica condivisione. A qualunque fallimento
    /// nessuno dei due viene consegnato e i file parziali sono rimossi.
    /// </summary>
    Task<ExportResult> ExportAsync();

    /// <summary>Rimuove dalla cartella transitoria i file dell'export precedente.</summary>
    Task ClearTransientFilesAsync();
}
