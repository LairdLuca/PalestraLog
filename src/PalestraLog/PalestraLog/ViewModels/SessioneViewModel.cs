using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PalestraLog.Controls;
using PalestraLog.Models;
using PalestraLog.Models.Enums;
using PalestraLog.Services.Interfaces;

namespace PalestraLog.ViewModels;

/// <summary>
/// Singolo esercizio dentro una sessione (singola o super set). In modalità singolo
/// `Membri.Count == 1`. In modalità super set ce n'è uno per ciascun esercizio del gruppo:
/// il LAP cicla tra di loro, il timer e il contatore SET sono unici a livello di gruppo.
/// </summary>
public partial class MembroSuperSet : ObservableObject
{
    public Esercizio Esercizio { get; set; } = null!;
    public EsercizioGiorno EsercizioGiorno { get; set; } = null!;

    [ObservableProperty] private StatisticheEsercizio? _statistiche;
    [ObservableProperty] private EsercizioSessione? _esercizioSessione;
    [ObservableProperty] private bool _isAttivo;

    [ObservableProperty] private double _pesoCorrente;
    [ObservableProperty] private int _ripetizioniCorrenti;
    [ObservableProperty] private double _velocitaCorrente;
    [ObservableProperty] private double _inclinazioneCorrente;
    [ObservableProperty] private int _durataCardioCorrente;
    [ObservableProperty] private double _distanzaCorrente;

    [ObservableProperty] private ObservableCollection<SetRecord> _sets = [];

    public string Nome => Esercizio.Nome;
    public bool IsPesi => Esercizio.TipoEsercizio == TipoEsercizio.Pesi;
    public bool IsCorpoLibero => Esercizio.TipoEsercizio == TipoEsercizio.CorpoLibero;
    public bool IsCardio => Esercizio.TipoEsercizio == TipoEsercizio.Cardio;

    // Hold isometrico: corpo libero con target di durata anziché ripetizioni (es. plank).
    // L'input mostra DURATA SEC anziché REPS; il salvataggio popola DurataCardioSecondi
    // (riusato come "durata dichiarata dall'utente") invece di Ripetizioni.
    public bool IsHold => IsCorpoLibero && EsercizioGiorno.DurataTargetSecondi.HasValue;
}

public partial class SessioneViewModel : ObservableObject, IQueryAttributable, IDisposable
{
    private readonly ISessioneService _sessioneService;
    private readonly ISchedaService _schedaService;
    private readonly IStatisticheService _statisticheService;
    private readonly ITimerService _timerService;

    private int _eserciziGiornoIdTappato;
    private int _sessioneId;

    public SessioneViewModel(
        ISessioneService sessioneService,
        ISchedaService schedaService,
        IStatisticheService statisticheService,
        ITimerService timerService)
    {
        _sessioneService = sessioneService;
        _schedaService = schedaService;
        _statisticheService = statisticheService;
        _timerService = timerService;

        _timerService.Tick += OnTimerTick;
        _timerService.CountdownFinished += OnCountdownFinished;
    }

    // --- Stato membri (singolo o super set) ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MembroAttivo))]
    [NotifyPropertyChangedFor(nameof(IsCardio))]
    [NotifyPropertyChangedFor(nameof(IsPesi))]
    [NotifyPropertyChangedFor(nameof(IsCorpoLibero))]
    [NotifyPropertyChangedFor(nameof(ShowWarmupEnterButton))]
    private int _indiceMembroAttivo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MembroAttivo))]
    [NotifyPropertyChangedFor(nameof(IsModalitaSuperSet))]
    [NotifyPropertyChangedFor(nameof(LabelHeader))]
    [NotifyPropertyChangedFor(nameof(LabelHeaderTipo))]
    [NotifyPropertyChangedFor(nameof(LabelTipoSuperSet))]
    [NotifyPropertyChangedFor(nameof(IsCardio))]
    [NotifyPropertyChangedFor(nameof(IsPesi))]
    [NotifyPropertyChangedFor(nameof(IsCorpoLibero))]
    [NotifyPropertyChangedFor(nameof(ShowWarmupEnterButton))]
    private ObservableCollection<MembroSuperSet> _membri = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LabelHeader))]
    private GruppoEsercizi? _gruppo;

    public MembroSuperSet? MembroAttivo =>
        Membri.Count > 0 && IndiceMembroAttivo >= 0 && IndiceMembroAttivo < Membri.Count
            ? Membri[IndiceMembroAttivo]
            : null;

    public bool IsModalitaSuperSet => Membri.Count > 1;

    public string LabelTipoSuperSet => Membri.Count switch
    {
        2 => "SUPER SET",
        3 => "TRI-SET",
        >= 4 => "GIANT SET",
        _ => ""
    };

    public string LabelHeader => IsModalitaSuperSet
        ? (Gruppo?.Nome is { Length: > 0 } n ? n : LabelTipoSuperSet)
        : (MembroAttivo?.Nome ?? "");

    public string LabelHeaderTipo => IsModalitaSuperSet ? LabelTipoSuperSet : "ESERCIZIO";

    public bool IsPesi => MembroAttivo?.IsPesi ?? false;
    public bool IsCorpoLibero => MembroAttivo?.IsCorpoLibero ?? false;
    public bool IsCardio => MembroAttivo?.IsCardio ?? false;

    // --- Timer / display ---

    [ObservableProperty] private string _timerDisplay = "00:00.000";
    [ObservableProperty] private string _serieDisplay = "00:00.000";
    [ObservableProperty] private string _lapDisplay = "00:00.000";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LabelTastoCentrale))]
    [NotifyPropertyChangedFor(nameof(AuraState))]
    private bool _isTimerRunning;

    // --- Set / pallini ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PalliniFattiTarget))]
    [NotifyPropertyChangedFor(nameof(PalliniRimanenti))]
    [NotifyPropertyChangedFor(nameof(PalliniExtra))]
    [NotifyPropertyChangedFor(nameof(IsOltreLimite))]
    [NotifyPropertyChangedFor(nameof(LabelSet))]
    private int _numeroSetCorrente = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PalliniFattiTarget))]
    [NotifyPropertyChangedFor(nameof(PalliniRimanenti))]
    [NotifyPropertyChangedFor(nameof(PalliniExtra))]
    [NotifyPropertyChangedFor(nameof(IsOltreLimite))]
    [NotifyPropertyChangedFor(nameof(LabelSet))]
    private int _setTotali = 4;

    [ObservableProperty] private bool _isBusy;

    // --- Statistiche e tipo (delegate al MembroAttivo, conservati per binding cardio legacy) ---

    [ObservableProperty] private int _countdownSecondi = 60;
    [ObservableProperty] private bool _isCountdownMode;

    // --- Stato esercizio ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowFineButton))]
    [NotifyPropertyChangedFor(nameof(AuraState))]
    private bool _showFaticaSelector;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LabelTastoCentrale))]
    private bool _isEsercizioIniziato;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowFineButton))]
    private int _setLavoroCompletati;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowFineButton))]
    [NotifyPropertyChangedFor(nameof(ShowWarmupEnterButton))]
    [NotifyPropertyChangedFor(nameof(AuraState))]
    private bool _isWarmupMode;

    public bool ShowFineButton => SetLavoroCompletati > 0 && !ShowFaticaSelector && !IsWarmupMode;
    public bool ShowWarmupEnterButton => !IsWarmupMode && !IsCardio;

    public string LabelTastoCentrale =>
        !IsEsercizioIniziato ? "INIZIA"
        : IsTimerRunning ? "LAP"
        : "RIPRENDI";

    private int Fatti => Math.Max(0, NumeroSetCorrente - 1);

    public string PalliniFattiTarget =>
        string.Concat(Enumerable.Repeat("● ", Math.Min(Fatti, SetTotali)));

    public string PalliniRimanenti =>
        string.Concat(Enumerable.Repeat("○ ", Math.Max(0, SetTotali - Fatti)));

    public string PalliniExtra =>
        string.Concat(Enumerable.Repeat("● ", Math.Max(0, Fatti - SetTotali)));

    public bool IsOltreLimite => NumeroSetCorrente > SetTotali;

    public string LabelSet => $"SET {NumeroSetCorrente:D2}/{SetTotali:D2}";

    public AuraState AuraState =>
        ShowFaticaSelector || !IsTimerRunning ? AuraState.Idle :
        IsWarmupMode ? AuraState.Warmup : AuraState.SetRunning;

    // --- Fatica a cascata (un membro per volta in modalità super set) ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LabelDomandaFatica))]
    private int _indiceMembroFatica;

    public string LabelDomandaFatica =>
        IsModalitaSuperSet && IndiceMembroFatica < Membri.Count
            ? $"FATICA · {Membri[IndiceMembroFatica].Nome.ToUpper()}"
            : "COME TI SEI SENTITO?";

    // --- Routing / load ---

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        int newEsercizioGiornoId = 0, newSessioneId = 0;
        if (query.TryGetValue("esercizioGiornoId", out var egid))
            newEsercizioGiornoId = Convert.ToInt32(egid);
        if (query.TryGetValue("sessioneId", out var sid))
            newSessioneId = Convert.ToInt32(sid);

        // Stesso esercizio (esatto): no-op (Singleton VM, stato già valido)
        if (newEsercizioGiornoId > 0
            && newEsercizioGiornoId == _eserciziGiornoIdTappato
            && newSessioneId == _sessioneId)
            return;

        // Stessa sessione e l'esercizio è membro del super set già in carico:
        // basta cambiare attivo, niente reload.
        if (newSessioneId == _sessioneId && Membri.Count > 1)
        {
            var idx = -1;
            for (int i = 0; i < Membri.Count; i++)
                if (Membri[i].EsercizioGiorno.Id == newEsercizioGiornoId) { idx = i; break; }
            if (idx >= 0)
            {
                _eserciziGiornoIdTappato = newEsercizioGiornoId;
                CambiaAttivo(idx);
                return;
            }
        }

        ResetVmState();
        _eserciziGiornoIdTappato = newEsercizioGiornoId;
        _sessioneId = newSessioneId;
        LoadDataCommand.Execute(null);
    }

    private void ResetVmState()
    {
        _timerService.Reset();

        Membri = [];
        Gruppo = null;
        IndiceMembroAttivo = 0;
        IndiceMembroFatica = 0;

        TimerDisplay = "00:00.000";
        SerieDisplay = "00:00.000";
        LapDisplay = "00:00.000";

        IsTimerRunning = false;
        IsEsercizioIniziato = false;
        NumeroSetCorrente = 1;
        SetTotali = 4;
        SetLavoroCompletati = 0;

        IsWarmupMode = false;
        IsCountdownMode = false;
        CountdownSecondi = 60;
        ShowFaticaSelector = false;
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            if (_eserciziGiornoIdTappato <= 0 || _sessioneId <= 0)
                return;

            var egTappato = await _schedaService.GetEsercizioGiornoByIdAsync(_eserciziGiornoIdTappato);
            if (egTappato is null) return;

            // Determina se l'esercizio appartiene a un gruppo: in tal caso carica tutti i membri.
            IList<EsercizioGiorno> egDaCaricare;
            if (egTappato.GruppoId is int gruppoId)
            {
                var tuttiDelGiorno = await _schedaService.GetEserciziGiornoAsync(egTappato.GiornoId);
                egDaCaricare = tuttiDelGiorno.Where(eg => eg.GruppoId == gruppoId).ToList();
                var gruppi = await _schedaService.GetGruppiGiornoAsync(egTappato.GiornoId);
                Gruppo = gruppi.FirstOrDefault(g => g.Id == gruppoId);
            }
            else
            {
                egDaCaricare = [egTappato];
                Gruppo = null;
            }

            // Costruisci i Membri (statistiche + pre-compilazione dal target di scheda)
            var membri = new List<MembroSuperSet>();
            foreach (var eg in egDaCaricare)
            {
                var es = await _schedaService.GetEsercizioAsync(eg.EsercizioId);
                if (es is null) continue;
                var stat = await _statisticheService.GetStatisticheAsync(es.Id, eg.Id);

                membri.Add(new MembroSuperSet
                {
                    Esercizio = es,
                    EsercizioGiorno = eg,
                    Statistiche = stat,
                    PesoCorrente = stat?.DatiScheda?.PesoTarget ?? 0,
                    RipetizioniCorrenti = stat?.DatiScheda?.RipetizioniTarget ?? 0,
                    VelocitaCorrente = stat?.DatiScheda?.VelocitaTarget ?? 0,
                    InclinazioneCorrente = stat?.DatiScheda?.InclinazioneTarget ?? 0,
                    DurataCardioCorrente = stat?.DatiScheda?.DurataTargetSecondi ?? 0,
                });
            }

            Membri = new ObservableCollection<MembroSuperSet>(membri);

            // Attivo iniziale = quello tappato dall'utente
            int idxTappato = -1;
            for (int i = 0; i < Membri.Count; i++)
                if (Membri[i].EsercizioGiorno.Id == _eserciziGiornoIdTappato) { idxTappato = i; break; }
            CambiaAttivo(Math.Max(0, idxTappato));

            // SetTotali dal target del primo membro (in un super set tutti hanno stesso SerieTarget,
            // garantito da iter 1 al raggruppamento).
            SetTotali = Membri.FirstOrDefault()?.EsercizioGiorno.SerieTarget ?? 4;

            // Riprendi eventuali EsercizioSessione già aperti per i membri.
            bool almenoUnoAperto = false;
            DateTime? primaDataInizio = null;
            DateTime? ultimoTimestampSet = null;
            foreach (var m in Membri)
            {
                var apertoEs = await _sessioneService.GetEsercizioSessioneApertaAsync(_sessioneId, m.EsercizioGiorno.Id);
                if (apertoEs is null) continue;
                m.EsercizioSessione = apertoEs;
                var setEsistenti = await _sessioneService.GetSetRecordsAsync(apertoEs.Id);
                m.Sets = new ObservableCollection<SetRecord>(setEsistenti);
                almenoUnoAperto = true;

                if (primaDataInizio is null || apertoEs.DataInizio < primaDataInizio)
                    primaDataInizio = apertoEs.DataInizio;
                var maxLocale = setEsistenti.Count > 0 ? setEsistenti.Max(s => s.Timestamp) : (DateTime?)null;
                if (maxLocale is not null && (ultimoTimestampSet is null || maxLocale > ultimoTimestampSet))
                    ultimoTimestampSet = maxLocale;
            }

            if (almenoUnoAperto)
            {
                // SetLavoroCompletati = giri completi del super set = MIN dei set lavoro tra membri.
                var setLavoroPerMembro = Membri.Select(m => m.Sets.Count(s => !s.IsRiscaldamento)).ToList();
                var giriCompleti = setLavoroPerMembro.Min();
                SetLavoroCompletati = giriCompleti;
                NumeroSetCorrente = giriCompleti + 1;

                // Attivo riprendere = primo membro che ha meno set (è il prossimo a registrare).
                int idxRipresa = setLavoroPerMembro.FindIndex(c => c < setLavoroPerMembro.Max());
                if (idxRipresa < 0) idxRipresa = 0;
                CambiaAttivo(idxRipresa);

                IsEsercizioIniziato = true;

                var elapsed = primaDataInizio is not null ? DateTime.Now - primaDataInizio.Value : TimeSpan.Zero;
                if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;
                var lapStartFrom = ultimoTimestampSet ?? primaDataInizio ?? DateTime.Now;
                var lapElapsed = DateTime.Now - lapStartFrom;
                if (lapElapsed < TimeSpan.Zero) lapElapsed = TimeSpan.Zero;

                _timerService.Resume(elapsed, lapElapsed);
                IsTimerRunning = true;
            }
        }
        catch (Exception ex)
        {
            try
            {
                await Shell.Current.DisplayAlertAsync("Errore caricamento esercizio", ex.Message, "OK");
                await Shell.Current.GoToAsync("..");
            }
            catch { }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CambiaAttivo(int nuovoIndice)
    {
        if (nuovoIndice < 0 || nuovoIndice >= Membri.Count) return;
        IndiceMembroAttivo = nuovoIndice;
        for (int i = 0; i < Membri.Count; i++)
            Membri[i].IsAttivo = (i == nuovoIndice);
    }

    [RelayCommand]
    private void SwitchMembroAttivo(MembroSuperSet membro)
    {
        var idx = Membri.IndexOf(membro);
        if (idx >= 0) CambiaAttivo(idx);
    }

    // --- Lazy create EsercizioSessione (singolo o batch super set) ---

    private async Task EnsureEsercizioSessioneAsync()
    {
        if (Membri.Count == 0) return;
        if (Membri.All(m => m.EsercizioSessione is not null)) return;

        if (Gruppo is not null && Membri.Count > 1)
        {
            var creati = await _sessioneService.IniziaSuperSetSessioneAsync(
                _sessioneId, Gruppo.Id, Membri.Select(m => m.EsercizioGiorno).ToList());
            for (int i = 0; i < Math.Min(Membri.Count, creati.Count); i++)
                Membri[i].EsercizioSessione = creati[i];
        }
        else
        {
            var m = Membri[0];
            m.EsercizioSessione = await _sessioneService.IniziaEsercizioSessioneAsync(
                _sessioneId, m.Esercizio.Id, m.EsercizioGiorno.Id, 0);
        }
    }

    // --- Tasto centrale + LAP + pausa ---

    [RelayCommand]
    private async Task TastoCentraleAsync()
    {
        if (!IsEsercizioIniziato)
        {
            await EnsureEsercizioSessioneAsync();
            _timerService.Start();
            IsTimerRunning = true;
            IsEsercizioIniziato = true;
            return;
        }

        if (IsTimerRunning)
        {
            await RegistraSetAsync(IsWarmupMode);
            return;
        }

        _timerService.Start();
        IsTimerRunning = true;
    }

    [RelayCommand]
    private void ToggleTimer()
    {
        if (_timerService.IsRunning)
        {
            _timerService.Stop();
            IsTimerRunning = false;
        }
        else
        {
            _timerService.Start();
            IsTimerRunning = true;
        }
    }

    [RelayCommand]
    private async Task StartCountdownAsync()
    {
        await EnsureEsercizioSessioneAsync();
        IsCountdownMode = true;
        _timerService.StartCountdown(TimeSpan.FromSeconds(CountdownSecondi));
        IsTimerRunning = true;
        IsEsercizioIniziato = true;
    }

    [RelayCommand]
    private Task LapAsync() => RegistraSetAsync(IsWarmupMode);

    [RelayCommand]
    private void ToggleWarmup()
    {
        var attivo = MembroAttivo;
        if (attivo is null || attivo.IsCardio) return;

        IsWarmupMode = !IsWarmupMode;

        if (IsWarmupMode)
            attivo.PesoCorrente = attivo.IsPesi ? 20 : 0;
        else
            attivo.PesoCorrente = attivo.Statistiche?.DatiScheda?.PesoTarget ?? 0;
    }

    private async Task RegistraSetAsync(bool isRiscaldamento)
    {
        var attivo = MembroAttivo;
        if (attivo?.EsercizioSessione is null) return;

        var lapTime = _timerService.Lap();
        IsEsercizioIniziato = true;

        var setRecord = new SetRecord
        {
            EsercizioSessioneId = attivo.EsercizioSessione.Id,
            NumeroSet = isRiscaldamento ? 0 : NumeroSetCorrente,
            IsRiscaldamento = isRiscaldamento,
            DurataSecondi = (int)lapTime.TotalSeconds,
            Timestamp = DateTime.Now
        };

        if (attivo.IsHold)
        {
            // Hold isometrico: durata dichiarata, peso opzionale (zavorra), niente reps.
            setRecord.Peso = attivo.PesoCorrente > 0 ? attivo.PesoCorrente : null;
            setRecord.DurataCardioSecondi = attivo.DurataCardioCorrente > 0 ? attivo.DurataCardioCorrente : null;
        }
        else if (attivo.IsPesi || attivo.IsCorpoLibero)
        {
            setRecord.Peso = attivo.PesoCorrente > 0 ? attivo.PesoCorrente : null;
            setRecord.Ripetizioni = attivo.RipetizioniCorrenti > 0 ? attivo.RipetizioniCorrenti : null;
        }
        else if (attivo.IsCardio)
        {
            setRecord.Velocita = attivo.VelocitaCorrente > 0 ? attivo.VelocitaCorrente : null;
            setRecord.Inclinazione = attivo.InclinazioneCorrente > 0 ? attivo.InclinazioneCorrente : null;
            setRecord.DurataCardioSecondi = attivo.DurataCardioCorrente > 0 ? attivo.DurataCardioCorrente : null;
            setRecord.Distanza = attivo.DistanzaCorrente > 0 ? attivo.DistanzaCorrente : null;
        }

        await _sessioneService.SalvaSetRecordAsync(setRecord);
        attivo.Sets.Add(setRecord);

        if (!isRiscaldamento)
        {
            // Cicla al membro successivo. Quando torno al primo (giro completo),
            // incremento il contatore SET del super set.
            var nuovoIdx = (IndiceMembroAttivo + 1) % Membri.Count;
            CambiaAttivo(nuovoIdx);

            if (nuovoIdx == 0)
            {
                NumeroSetCorrente++;
                SetLavoroCompletati++;
            }
        }

        LapDisplay = FormatTimeSpan(lapTime);
    }

    private static string FormatTimeSpan(TimeSpan ts) =>
        ts.Hours > 0
            ? ts.ToString(@"h\:mm\:ss\.fff")
            : ts.ToString(@"mm\:ss\.fff");

    // --- Stepper peso/reps (operano sul MembroAttivo) ---

    [RelayCommand]
    private void AumentaPeso(string incremento)
    {
        var attivo = MembroAttivo;
        if (attivo is null) return;
        if (double.TryParse(incremento, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var val))
            attivo.PesoCorrente += val;
    }

    [RelayCommand]
    private void DiminuisciPeso(string incremento)
    {
        var attivo = MembroAttivo;
        if (attivo is null) return;
        if (double.TryParse(incremento, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var val))
            attivo.PesoCorrente = Math.Max(0, attivo.PesoCorrente - val);
    }

    [RelayCommand]
    private void AumentaReps()
    {
        var attivo = MembroAttivo;
        if (attivo is not null) attivo.RipetizioniCorrenti++;
    }

    [RelayCommand]
    private void DiminuisciReps()
    {
        var attivo = MembroAttivo;
        if (attivo is not null) attivo.RipetizioniCorrenti = Math.Max(0, attivo.RipetizioniCorrenti - 1);
    }

    [RelayCommand]
    private void AumentaDurata(string incremento)
    {
        var attivo = MembroAttivo;
        if (attivo is null) return;
        if (int.TryParse(incremento, out var val))
            attivo.DurataCardioCorrente += val;
    }

    [RelayCommand]
    private void DiminuisciDurata(string incremento)
    {
        var attivo = MembroAttivo;
        if (attivo is null) return;
        if (int.TryParse(incremento, out var val))
            attivo.DurataCardioCorrente = Math.Max(0, attivo.DurataCardioCorrente - val);
    }

    // --- Fatica a cascata: in modalità super set chiede una fatica per ogni membro ---

    [RelayCommand]
    private void MostraFatica()
    {
        _timerService.Stop();
        IsTimerRunning = false;
        IndiceMembroFatica = 0;
        OnPropertyChanged(nameof(LabelDomandaFatica));
        ShowFaticaSelector = true;
    }

    [RelayCommand]
    private async Task SelezionaFaticaAsync(string livelloStr)
    {
        if (!int.TryParse(livelloStr, out var livello)) return;
        await ChiudiFaticaMembroCorrenteAsync(livello);
    }

    [RelayCommand]
    private async Task SaltaFaticaAsync()
    {
        await ChiudiFaticaMembroCorrenteAsync(0);
    }

    private async Task ChiudiFaticaMembroCorrenteAsync(int fatica)
    {
        if (IndiceMembroFatica < 0 || IndiceMembroFatica >= Membri.Count) return;

        var m = Membri[IndiceMembroFatica];
        if (m.EsercizioSessione is not null)
            await _sessioneService.FineEsercizioSessioneAsync(m.EsercizioSessione.Id, fatica);

        IndiceMembroFatica++;

        if (IndiceMembroFatica < Membri.Count)
        {
            // Ancora membri da chiudere: aggiorna la label per il prossimo.
            OnPropertyChanged(nameof(LabelDomandaFatica));
        }
        else
        {
            FinalizzaEsercizio();
            await Shell.Current.GoToAsync("..");
        }
    }

    private void FinalizzaEsercizio()
    {
        ResetVmState();
        _eserciziGiornoIdTappato = 0;
        _sessioneId = 0;
    }

    // --- Eventi timer ---

    private void OnTimerTick(object? sender, TimeSpan elapsed)
    {
        TimerDisplay = FormatTimeSpan(elapsed);
        SerieDisplay = FormatTimeSpan(_timerService.LapElapsed);
    }

    private async void OnCountdownFinished(object? sender, EventArgs e)
    {
        IsTimerRunning = false;
        IsCountdownMode = false;

        Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));
        await Shell.Current.DisplayAlertAsync("Tempo!", "Il countdown è terminato", "OK");
    }

    public void Dispose()
    {
        _timerService.Tick -= OnTimerTick;
        _timerService.CountdownFinished -= OnCountdownFinished;
        if (_timerService is IDisposable disposable)
            disposable.Dispose();
        GC.SuppressFinalize(this);
    }
}
