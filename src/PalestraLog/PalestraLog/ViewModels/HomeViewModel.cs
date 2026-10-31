using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PalestraLog.Models;
using PalestraLog.Services.Interfaces;

namespace PalestraLog.ViewModels;

public partial class HomeViewModel(ISchedaService schedaService, IExportService exportService) : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Scheda> _schede = [];

    [ObservableProperty]
    private Scheda? _schedaAttiva;

    [ObservableProperty]
    private bool _isBusy;

    [RelayCommand]
    private async Task LoadSchedeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var lista = await schedaService.GetSchedeAsync();
            Schede = new ObservableCollection<Scheda>(lista);
            SchedaAttiva = lista.FirstOrDefault(s => s.IsAttiva);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SetAttivaAsync(Scheda scheda)
    {
        await schedaService.SetSchedaAttivaAsync(scheda.Id);
        await LoadSchedeAsync();
    }

    [RelayCommand]
    private async Task NavigateToGiorniAsync(Scheda scheda)
    {
        await Shell.Current.GoToAsync($"giornoList?schedaId={scheda.Id}&schedaNome={Uri.EscapeDataString(scheda.Nome)}");
    }

    [RelayCommand]
    private async Task IniziaAllenamentoAsync()
    {
        if (SchedaAttiva is null) return;
        await NavigateToGiorniAsync(SchedaAttiva);
    }

    [RelayCommand]
    private async Task CreaSchedaAsync()
    {
        await Shell.Current.GoToAsync("schedaForm");
    }

    [RelayCommand]
    private async Task ModificaSchedaAsync(Scheda scheda)
    {
        await Shell.Current.GoToAsync($"schedaForm?schedaId={scheda.Id}");
    }

    private const string TestoEsporta = "ESPORTA BACKUP";

    // Il bottone si disattiva da solo mentre il comando lavora; il testo lo rende visibile.
    [ObservableProperty]
    private string _testoBottoneExport = TestoEsporta;

    // PROVVISORIO: il comando di export vive sulla Home solo finché non esiste la superficie
    // Impostazioni, che è la sua destinazione definitiva. Spostandolo, i messaggi d'esito vanno con lui.
    [RelayCommand]
    private async Task EsportaBackupAsync()
    {
        ExportResult esito;
        TestoBottoneExport = "PREPARAZIONE DEL BACKUP…";
        try
        {
            esito = await exportService.ExportAsync();
        }
        finally
        {
            TestoBottoneExport = TestoEsporta;
        }

        if (esito.HandedToSystem)
        {
            // Su Android il foglio di condivisione non dice all'app se l'utente ha completato o
            // annullato, e la chiamata ritorna prima che il foglio sia chiuso: il messaggio si
            // apre dietro al foglio e si legge al ritorno nell'app. Su iOS la chiamata attende la
            // chiusura del foglio. In entrambi i casi l'esito dichiara i due casi, senza affermare
            // un backup che l'app non può sapere.
            await Shell.Current.DisplayAlertAsync(
                "Il backup esiste solo se hai completato l'invio",
                "I due file, i dati e la copia del database, sono stati affidati al sistema.\n\n" +
                "Se hai scelto una destinazione e completato il salvataggio o l'invio, il backup esiste lì. " +
                "L'app non ne conserva una copia.\n\n" +
                "Se non l'hai completato, perché hai annullato o perché il foglio di condivisione non è comparso, " +
                "il backup NON esiste: ripeti l'export.\n\n" +
                "Il sistema non comunica all'app quale delle due cose è successa.",
                "Ho capito");
            return;
        }

        var fase = esito.FailedStage switch
        {
            ExportStage.DataFile => "non è stato possibile produrre il file dei dati",
            ExportStage.Snapshot => "non è stato possibile produrre la copia del database",
            ExportStage.Delivery => "non è stato possibile consegnare i file al sistema",
            _ => "l'export si è interrotto"
        };
        var dettaglio = string.IsNullOrWhiteSpace(esito.Error)
            ? ""
            : $"\n\nDettaglio tecnico, per chi sviluppa l'app: {esito.Error}";
        await Shell.Current.DisplayAlertAsync(
            "Backup non riuscito",
            "Il backup NON esiste: non è stato consegnato nessun file, nemmeno quello eventualmente riuscito.\n\n" +
            $"Cosa è successo: {fase}. I dati nell'app non sono stati toccati: puoi riprovare." +
            dettaglio,
            "OK");
    }

    [RelayCommand]
    private async Task DeleteSchedaAsync(Scheda scheda)
    {
        bool conferma = await Shell.Current.DisplayAlertAsync(
            "Elimina Scheda",
            $"Sei sicuro di voler eliminare \"{scheda.Nome}\"?",
            "Elimina", "Annulla");

        if (!conferma) return;
        await schedaService.DeleteSchedaAsync(scheda.Id);
        await LoadSchedeAsync();
    }
}
