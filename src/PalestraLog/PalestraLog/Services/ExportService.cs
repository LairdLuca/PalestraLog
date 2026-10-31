using System.Diagnostics;
using System.Globalization;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using PalestraLog.Services.Interfaces;

namespace PalestraLog.Services;

/// <summary>
/// Produce i due file del backup (AD-8) e li consegna al foglio di condivisione di sistema in
/// un'unica operazione. L'app non conserva ciò che produce (AD-16): i file vivono in una sola
/// cartella transitoria nella cache, che contiene al più la coppia dell'ultimo export.
/// </summary>
public class ExportService(IBackupService backup) : IExportService
{
    private static string TransientFolder => Path.Combine(TransientRoot, "export");

#if ANDROID
    // La cartella va in una posizione che la condivisione di MAUI espone sul posto: altrove MAUI
    // farebbe una propria copia di ogni file in una cartella che nessuno svuota. Dalla versione 7
    // di Android è la cache interna; prima solo la cache esterna, se la memoria è montata.
    private static string TransientRoot =>
        !OperatingSystem.IsAndroidVersionAtLeast(24)
        && Android.App.Application.Context.ExternalCacheDir?.AbsolutePath is { } externalCache
            ? externalCache
            : FileSystem.CacheDirectory;
#else
    private static string TransientRoot => FileSystem.CacheDirectory;
#endif

    // La pulizia all'avvio non deve cancellare i file di un export appena cominciato.
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<ExportResult> ExportAsync()
    {
        await _gate.WaitAsync();
        var stage = ExportStage.DataFile;
        try
        {
            // Secondi nel nome: due export ravvicinati non producono mai lo stesso file, né lo
            // stesso indirizzo di condivisione. Lo stesso istante nei due nomi lega la coppia.
            var stamp = DateTime.Now.ToString("yyyy-MM-dd-HHmmss", CultureInfo.InvariantCulture);
            var dataPath = Path.Combine(TransientFolder, $"PalestraLog-{stamp}-dati.json");
            var snapshotPath = Path.Combine(TransientFolder, $"PalestraLog-{stamp}-snapshot.db3");

            // Lettura, traduzione e scrittura fuori dal thread dell'interfaccia.
            await Task.Run(async () =>
            {
                // L'app ricevente può leggere i file dopo che la condivisione è ritornata: quelli
                // dell'export precedente si rimuovono solo ora, non al termine della consegna.
                DeleteTransientFolder();
                Directory.CreateDirectory(TransientFolder);

                await using (var stream = File.Create(dataPath))
                    await backup.WriteDataFileAsync(stream);

                stage = ExportStage.Snapshot;
                await backup.WriteSnapshotAsync(snapshotPath);
            });

            // Una sola richiesta per entrambi i file. Su Android la chiamata ritorna appena il
            // foglio è stato richiesto e non riporta né l'annullamento né il completamento.
            stage = ExportStage.Delivery;
            await Share.Default.RequestAsync(new ShareMultipleFilesRequest
            {
                Title = "Salva il backup di PalestraLog",
                Files =
                [
                    new ShareFile(dataPath, "application/json"),
                    new ShareFile(snapshotPath, "application/octet-stream")
                ]
            });

            return new ExportResult(HandedToSystem: true);
        }
        catch (Exception ex)
        {
            // Tutto-o-niente: il foglio non si è aperto, quindi nulla è uscito; anche il file
            // riuscito si cancella, perché mezzo backup non va consegnato.
            Debug.WriteLine($"[Export] fallito nella fase {stage}: {ex}");
            DeleteTransientFolder();
            return new ExportResult(HandedToSystem: false, stage, ex.Message);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearTransientFilesAsync()
    {
        await _gate.WaitAsync();
        try
        {
            DeleteTransientFolder();
        }
        finally
        {
            _gate.Release();
        }
    }

    // Una pulizia non riuscita non blocca l'export: si ritenta all'apertura dell'app e
    // all'export successivo.
    private static void DeleteTransientFolder()
    {
        try
        {
            if (Directory.Exists(TransientFolder))
                Directory.Delete(TransientFolder, recursive: true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Export] pulizia della cartella transitoria non riuscita: {ex}");
        }
    }
}
