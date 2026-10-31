using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PalestraLog.Models;
using PalestraLog.Services.Interfaces;

namespace PalestraLog.ViewModels;

public partial class GiornoInput : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _nome;

    public GiornoInput(string nome) => _nome = nome;

    public GiornoInput(int id, string nome)
    {
        _id = id;
        _nome = nome;
    }
}

public partial class SchedaFormViewModel(ISchedaService schedaService) : ObservableObject, IQueryAttributable
{
    private int _schedaIdEdit;
    private readonly HashSet<int> _giorniOriginaliIds = [];

    [ObservableProperty]
    private string _nome = string.Empty;

    [ObservableProperty]
    private string _descrizione = string.Empty;

    [ObservableProperty]
    private ObservableCollection<GiornoInput> _giorni = [new("Giorno 1")];

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errore = string.Empty;

    [ObservableProperty]
    private bool _isEdit;

    [ObservableProperty]
    private string _titlePagina = "Nuova Scheda";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("schedaId", out var idObj) && int.TryParse(idObj?.ToString(), out var id) && id > 0)
        {
            _schedaIdEdit = id;
            IsEdit = true;
            TitlePagina = "Modifica Scheda";
            _ = LoadEsistenteAsync();
        }
    }

    private async Task LoadEsistenteAsync()
    {
        var scheda = await schedaService.GetSchedaByIdAsync(_schedaIdEdit);
        if (scheda is null) return;
        Nome = scheda.Nome;
        Descrizione = scheda.Descrizione ?? string.Empty;

        var giorni = await schedaService.GetGiorniAsync(_schedaIdEdit);
        Giorni = new ObservableCollection<GiornoInput>(
            giorni.Select(g => new GiornoInput(g.Id, g.Nome)));
        _giorniOriginaliIds.Clear();
        foreach (var g in giorni) _giorniOriginaliIds.Add(g.Id);
    }

    [RelayCommand]
    private void AggiungiGiorno()
    {
        Giorni.Add(new GiornoInput($"Giorno {Giorni.Count + 1}"));
    }

    [RelayCommand]
    private void RimuoviGiorno(GiornoInput giorno)
    {
        if (Giorni.Count <= 1) return;
        Giorni.Remove(giorno);
    }

    [RelayCommand]
    private async Task SalvaAsync()
    {
        Errore = string.Empty;

        if (string.IsNullOrWhiteSpace(Nome))
        {
            Errore = "Inserisci un nome per la scheda";
            return;
        }

        IsBusy = true;
        try
        {
            Scheda scheda;
            if (IsEdit)
            {
                scheda = await schedaService.GetSchedaByIdAsync(_schedaIdEdit)
                    ?? throw new InvalidOperationException("Scheda non trovata");
                scheda.Nome = Nome.Trim();
                scheda.Descrizione = string.IsNullOrWhiteSpace(Descrizione) ? null : Descrizione.Trim();
                await schedaService.SaveSchedaAsync(scheda);
            }
            else
            {
                scheda = new Scheda
                {
                    Nome = Nome.Trim(),
                    Descrizione = string.IsNullOrWhiteSpace(Descrizione) ? null : Descrizione.Trim(),
                    DataCreazione = DateTime.Now,
                    IsAttiva = false
                };
                await schedaService.SaveSchedaAsync(scheda);
            }

            var giorniCorrentiIds = new HashSet<int>();
            for (int i = 0; i < Giorni.Count; i++)
            {
                var input = Giorni[i];
                var giorno = new Giorno
                {
                    Id = input.Id,
                    SchedaId = scheda.Id,
                    Nome = string.IsNullOrWhiteSpace(input.Nome) ? $"Giorno {i + 1}" : input.Nome.Trim(),
                    Ordine = i
                };
                await schedaService.SaveGiornoAsync(giorno);
                input.Id = giorno.Id;
                giorniCorrentiIds.Add(giorno.Id);
            }

            if (IsEdit)
            {
                foreach (var idOriginale in _giorniOriginaliIds)
                {
                    if (!giorniCorrentiIds.Contains(idOriginale))
                        await schedaService.DeleteGiornoAsync(idOriginale);
                }
            }

            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AnnullaAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
