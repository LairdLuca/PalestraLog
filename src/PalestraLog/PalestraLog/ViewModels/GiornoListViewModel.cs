using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PalestraLog.Models;
using PalestraLog.Services.Interfaces;

namespace PalestraLog.ViewModels;

public partial class GiornoListViewModel(ISchedaService schedaService) : ObservableObject, IQueryAttributable
{
    private int _schedaId;

    [ObservableProperty]
    private string _schedaNome = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Giorno> _giorni = [];

    [ObservableProperty]
    private bool _isBusy;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("schedaId", out var id))
            _schedaId = Convert.ToInt32(id);
        if (query.TryGetValue("schedaNome", out var nome))
            SchedaNome = Uri.UnescapeDataString(nome?.ToString() ?? "");

        LoadGiorniCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadGiorniAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var lista = await schedaService.GetGiorniAsync(_schedaId);
            Giorni = new ObservableCollection<Giorno>(lista);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SelectGiornoAsync(Giorno giorno)
    {
        await Shell.Current.GoToAsync(
            $"esercizioList?giornoId={giorno.Id}&giornoNome={Uri.EscapeDataString(giorno.Nome)}&schedaId={_schedaId}");
    }

    [RelayCommand]
    private async Task RinominaGiornoAsync(Giorno giorno)
    {
        var nuovoNome = await Shell.Current.DisplayPromptAsync(
            "Rinomina giorno",
            "Nuovo nome:",
            initialValue: giorno.Nome,
            accept: "Salva",
            cancel: "Annulla",
            maxLength: 100);

        if (string.IsNullOrWhiteSpace(nuovoNome)) return;
        giorno.Nome = nuovoNome.Trim();
        await schedaService.SaveGiornoAsync(giorno);
        await LoadGiorniAsync();
    }

    [RelayCommand]
    private async Task EliminaGiornoAsync(Giorno giorno)
    {
        var esercizi = await schedaService.GetEserciziGiornoAsync(giorno.Id);
        string msg = esercizi.Count > 0
            ? $"Eliminando \"{giorno.Nome}\" rimuoverai anche {esercizi.Count} esercizi associati. Procedere?"
            : $"Eliminare \"{giorno.Nome}\"?";

        bool ok = await Shell.Current.DisplayAlertAsync(
            "Elimina giorno", msg, "Elimina", "Annulla");

        if (!ok) return;
        await schedaService.DeleteGiornoAsync(giorno.Id);
        await LoadGiorniAsync();
    }

    [RelayCommand]
    private async Task AggiungiGiornoAsync()
    {
        var nome = await Shell.Current.DisplayPromptAsync(
            "Nuovo giorno",
            "Nome:",
            initialValue: $"Giorno {Giorni.Count + 1}",
            accept: "Crea",
            cancel: "Annulla",
            maxLength: 100);

        if (string.IsNullOrWhiteSpace(nome)) return;

        var giorno = new Giorno
        {
            SchedaId = _schedaId,
            Nome = nome.Trim(),
            Ordine = Giorni.Count
        };
        await schedaService.SaveGiornoAsync(giorno);
        await LoadGiorniAsync();
    }
}
