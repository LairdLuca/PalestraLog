using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PalestraLog.Models;
using PalestraLog.Models.Enums;
using PalestraLog.Services.Interfaces;

namespace PalestraLog.ViewModels;

public partial class EsercizioFormViewModel(ISchedaService schedaService) : ObservableObject, IQueryAttributable
{
    private int _giornoId;
    private int _esercizioGiornoIdEdit;
    private int _esercizioIdEdit;

    [ObservableProperty] private string _nome = string.Empty;
    [ObservableProperty] private int _tipoSelezionato; // 0=Pesi, 1=CorpoLibero, 2=Cardio
    [ObservableProperty] private bool _isTemporaneo;

    // Target
    [ObservableProperty] private int _serieTarget;
    [ObservableProperty] private int _ripetizioniTarget;
    [ObservableProperty] private double _pesoTarget;
    [ObservableProperty] private int _durataTargetSecondi;
    [ObservableProperty] private double _velocitaTarget;
    [ObservableProperty] private double _inclinazioneTarget;
    [ObservableProperty] private double _distanzaTarget;
    [ObservableProperty] private string _note = string.Empty;

    [ObservableProperty] private bool _isPesi = true;
    [ObservableProperty] private bool _isCorpoLibero;
    [ObservableProperty] private bool _isCardio;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _errore = string.Empty;

    [ObservableProperty] private bool _isEdit;
    [ObservableProperty] private string _titlePagina = "Nuovo Esercizio";

    public string[] TipiEsercizio => ["Pesi", "Corpo Libero", "Cardio"];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("giornoId", out var gObj) && int.TryParse(gObj?.ToString(), out var gid))
            _giornoId = gid;

        if (query.TryGetValue("esercizioGiornoId", out var egObj)
            && int.TryParse(egObj?.ToString(), out var egid)
            && egid > 0)
        {
            _esercizioGiornoIdEdit = egid;
            IsEdit = true;
            TitlePagina = "Modifica Esercizio";
            _ = LoadEsistenteAsync();
        }
    }

    private async Task LoadEsistenteAsync()
    {
        var eg = await schedaService.GetEsercizioGiornoByIdAsync(_esercizioGiornoIdEdit);
        if (eg is null) return;
        _esercizioIdEdit = eg.EsercizioId;
        _giornoId = eg.GiornoId;

        var es = await schedaService.GetEsercizioAsync(eg.EsercizioId);
        if (es is null) return;

        Nome = es.Nome;
        TipoSelezionato = (int)es.TipoEsercizio;
        IsTemporaneo = eg.IsTemporaneo;

        SerieTarget = eg.SerieTarget ?? 0;
        RipetizioniTarget = eg.RipetizioniTarget ?? 0;
        PesoTarget = eg.PesoTarget ?? 0;
        DurataTargetSecondi = eg.DurataTargetSecondi ?? 0;
        VelocitaTarget = eg.VelocitaTarget ?? 0;
        InclinazioneTarget = eg.InclinazioneTarget ?? 0;
        DistanzaTarget = eg.DistanzaTarget ?? 0;
        Note = eg.Note ?? string.Empty;
    }

    partial void OnTipoSelezionatoChanged(int value)
    {
        IsPesi = value == 0;
        IsCorpoLibero = value == 1;
        IsCardio = value == 2;
    }

    [RelayCommand]
    private async Task SalvaAsync()
    {
        Errore = string.Empty;

        if (string.IsNullOrWhiteSpace(Nome))
        {
            Errore = "Inserisci un nome per l'esercizio";
            return;
        }

        IsBusy = true;
        try
        {
            Esercizio esercizio;
            EsercizioGiorno eg;

            if (IsEdit)
            {
                esercizio = await schedaService.GetEsercizioAsync(_esercizioIdEdit)
                    ?? throw new InvalidOperationException("Esercizio non trovato");
                esercizio.Nome = Nome.Trim();
                // Tipo non modificabile in Edit (Picker disabilitato)
                await schedaService.SaveEsercizioAsync(esercizio);

                eg = await schedaService.GetEsercizioGiornoByIdAsync(_esercizioGiornoIdEdit)
                    ?? throw new InvalidOperationException("EsercizioGiorno non trovato");
                eg.IsTemporaneo = IsTemporaneo;
                eg.SerieTarget = SerieTarget > 0 ? SerieTarget : null;
                eg.RipetizioniTarget = RipetizioniTarget > 0 ? RipetizioniTarget : null;
                eg.PesoTarget = PesoTarget > 0 ? PesoTarget : null;
                eg.DurataTargetSecondi = DurataTargetSecondi > 0 ? DurataTargetSecondi : null;
                eg.VelocitaTarget = VelocitaTarget > 0 ? VelocitaTarget : null;
                eg.InclinazioneTarget = InclinazioneTarget > 0 ? InclinazioneTarget : null;
                eg.DistanzaTarget = DistanzaTarget > 0 ? DistanzaTarget : null;
                eg.Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim();
                await schedaService.SaveEsercizioGiornoAsync(eg);
            }
            else
            {
                esercizio = new Esercizio
                {
                    Nome = Nome.Trim(),
                    TipoEsercizio = (TipoEsercizio)TipoSelezionato
                };
                await schedaService.SaveEsercizioAsync(esercizio);

                eg = new EsercizioGiorno
                {
                    EsercizioId = esercizio.Id,
                    GiornoId = _giornoId,
                    IsTemporaneo = IsTemporaneo,
                    SerieTarget = SerieTarget > 0 ? SerieTarget : null,
                    RipetizioniTarget = RipetizioniTarget > 0 ? RipetizioniTarget : null,
                    PesoTarget = PesoTarget > 0 ? PesoTarget : null,
                    DurataTargetSecondi = DurataTargetSecondi > 0 ? DurataTargetSecondi : null,
                    VelocitaTarget = VelocitaTarget > 0 ? VelocitaTarget : null,
                    InclinazioneTarget = InclinazioneTarget > 0 ? InclinazioneTarget : null,
                    DistanzaTarget = DistanzaTarget > 0 ? DistanzaTarget : null,
                    Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim()
                };

                var esistenti = await schedaService.GetEserciziGiornoAsync(_giornoId);
                eg.Ordine = esistenti.Count;

                await schedaService.SaveEsercizioGiornoAsync(eg);
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
