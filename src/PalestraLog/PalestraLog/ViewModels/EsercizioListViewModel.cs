using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PalestraLog.Models;
using PalestraLog.Models.Enums;
using PalestraLog.Services.Interfaces;

namespace PalestraLog.ViewModels;

/// <summary>
/// DTO che combina l'esercizio dal catalogo con i dati del ponte giorno
/// </summary>
public partial class EsercizioConTarget : ObservableObject
{
    public Esercizio Esercizio { get; set; } = null!;
    public EsercizioGiorno EsercizioGiorno { get; set; } = null!;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDaFare))]
    [NotifyPropertyChangedFor(nameof(IsInCorso))]
    [NotifyPropertyChangedFor(nameof(IsFatto))]
    private StatoEsercizioSessione _stato;

    public string Nome => Esercizio.Nome;
    public string TargetDescrizione => FormatTarget();
    public bool IsInGruppo => EsercizioGiorno.GruppoId.HasValue;

    public bool IsDaFare => Stato == StatoEsercizioSessione.DaFare;
    public bool IsInCorso => Stato == StatoEsercizioSessione.InCorso;
    public bool IsFatto => Stato == StatoEsercizioSessione.Fatto;

    private string FormatTarget()
    {
        var eg = EsercizioGiorno;
        var parti = new List<string>();

        if (eg.SerieTarget.HasValue && eg.RipetizioniTarget.HasValue)
            parti.Add($"{eg.SerieTarget}x{eg.RipetizioniTarget}");
        if (eg.PesoTarget.HasValue)
            parti.Add($"{eg.PesoTarget}kg");
        if (eg.DurataTargetSecondi.HasValue)
            parti.Add($"{TimeSpan.FromSeconds(eg.DurataTargetSecondi.Value):mm\\:ss}");
        if (eg.VelocitaTarget.HasValue)
            parti.Add($"{eg.VelocitaTarget}km/h");
        if (!string.IsNullOrEmpty(eg.Note))
            parti.Add(eg.Note);

        return parti.Count > 0 ? string.Join(" · ", parti) : "Nessun target";
    }
}

/// <summary>
/// Elemento della lista esercizi del giorno: o un esercizio singolo o un gruppo
/// (super set / tri-set / giant set). Reso polimorfo via DataTemplateSelector nella View.
/// </summary>
public abstract partial class BloccoLista : ObservableObject { }

public partial class BloccoSingolo : BloccoLista
{
    public EsercizioConTarget Esercizio { get; set; } = null!;
}

public partial class BloccoGruppo : BloccoLista
{
    public GruppoEsercizi Gruppo { get; set; } = null!;
    public List<EsercizioConTarget> Membri { get; set; } = [];

    public string LabelTipo => Membri.Count switch
    {
        2 => "SUPER SET",
        3 => "TRI-SET",
        _ => "GIANT SET"
    };

    public string DisplayName => string.IsNullOrWhiteSpace(Gruppo.Nome)
        ? LabelTipo
        : $"{LabelTipo} · {Gruppo.Nome}";
}

public partial class EsercizioListViewModel(
    ISchedaService schedaService,
    ISessioneService sessioneService) : ObservableObject, IQueryAttributable
{
    private int _giornoId;
    private int _schedaId;
    private Sessione? _sessioneCorrente;

    [ObservableProperty]
    private string _giornoNome = string.Empty;

    [ObservableProperty]
    private ObservableCollection<BloccoLista> _blocchi = [];

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _hasSessioneAttiva;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("giornoId", out var id))
            _giornoId = Convert.ToInt32(id);
        if (query.TryGetValue("giornoNome", out var nome))
            GiornoNome = Uri.UnescapeDataString(nome?.ToString() ?? "");
        if (query.TryGetValue("schedaId", out var sid))
            _schedaId = Convert.ToInt32(sid);

        LoadEserciziCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadEserciziAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var eserciziGiorno = await schedaService.GetEserciziGiornoAsync(_giornoId);
            var gruppi = await schedaService.GetGruppiGiornoAsync(_giornoId);
            var gruppiById = gruppi.ToDictionary(g => g.Id);

            var listaFlat = new List<EsercizioConTarget>();
            foreach (var eg in eserciziGiorno)
            {
                var esercizio = await schedaService.GetEsercizioAsync(eg.EsercizioId);
                if (esercizio is not null)
                {
                    listaFlat.Add(new EsercizioConTarget
                    {
                        Esercizio = esercizio,
                        EsercizioGiorno = eg
                    });
                }
            }

            // Stati sessione (come prima): lookup per EserciziGiornoId.
            var ses = await sessioneService.GetSessioneCorrenteAsync();
            _sessioneCorrente = ses?.GiornoId == _giornoId ? ses : null;
            HasSessioneAttiva = _sessioneCorrente is not null;

            if (_sessioneCorrente is not null)
            {
                var esSessione = await sessioneService.GetEserciziSessioneAsync(_sessioneCorrente.Id);
                var ultimoPerEg = esSessione
                    .GroupBy(es => es.EserciziGiornoId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(es => es.DataInizio).First());

                foreach (var item in listaFlat)
                {
                    if (ultimoPerEg.TryGetValue(item.EsercizioGiorno.Id, out var es))
                        item.Stato = es.DataFine is null
                            ? StatoEsercizioSessione.InCorso
                            : StatoEsercizioSessione.Fatto;
                }
            }

            // Costruzione blocchi: scorriamo la lista flat (già ordinata per Ordine);
            // i membri di uno stesso gruppo (consecutivi per costruzione) finiscono in un BloccoGruppo,
            // gli altri in BloccoSingolo.
            var nuoviBlocchi = new List<BloccoLista>();
            var gruppiInseriti = new HashSet<int>();
            foreach (var item in listaFlat)
            {
                if (item.EsercizioGiorno.GruppoId is int gId && gruppiById.TryGetValue(gId, out var gruppo))
                {
                    if (gruppiInseriti.Contains(gId)) continue;
                    var membri = listaFlat.Where(x => x.EsercizioGiorno.GruppoId == gId).ToList();
                    if (membri.Count >= 2)
                    {
                        nuoviBlocchi.Add(new BloccoGruppo { Gruppo = gruppo, Membri = membri });
                        gruppiInseriti.Add(gId);
                    }
                    else
                    {
                        // Gruppo orfano (1 membro): trattalo come singolo. Non dovrebbe accadere
                        // grazie all'auto-scioglimento, ma è una difesa.
                        nuoviBlocchi.Add(new BloccoSingolo { Esercizio = item });
                    }
                }
                else
                {
                    nuoviBlocchi.Add(new BloccoSingolo { Esercizio = item });
                }
            }

            // Refresh in-place se la struttura è la stessa: aggiorna gli stati interni
            // senza rigenerare la collection (anti-flicker). Altrimenti sostituisce.
            if (StruttureUguali(Blocchi, nuoviBlocchi))
            {
                AggiornaStatiInPlace(Blocchi, nuoviBlocchi);
            }
            else
            {
                Blocchi = new ObservableCollection<BloccoLista>(nuoviBlocchi);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static bool StruttureUguali(IList<BloccoLista> a, IList<BloccoLista> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i].GetType() != b[i].GetType()) return false;
            switch (a[i], b[i])
            {
                case (BloccoSingolo sa, BloccoSingolo sb):
                    if (sa.Esercizio.EsercizioGiorno.Id != sb.Esercizio.EsercizioGiorno.Id) return false;
                    if (sa.Esercizio.Nome != sb.Esercizio.Nome) return false;
                    if (sa.Esercizio.TargetDescrizione != sb.Esercizio.TargetDescrizione) return false;
                    break;
                case (BloccoGruppo ga, BloccoGruppo gb):
                    if (ga.Gruppo.Id != gb.Gruppo.Id) return false;
                    if (ga.Gruppo.Nome != gb.Gruppo.Nome) return false;
                    if (ga.Membri.Count != gb.Membri.Count) return false;
                    for (int j = 0; j < ga.Membri.Count; j++)
                    {
                        if (ga.Membri[j].EsercizioGiorno.Id != gb.Membri[j].EsercizioGiorno.Id) return false;
                        if (ga.Membri[j].Nome != gb.Membri[j].Nome) return false;
                        if (ga.Membri[j].TargetDescrizione != gb.Membri[j].TargetDescrizione) return false;
                    }
                    break;
            }
        }
        return true;
    }

    private static void AggiornaStatiInPlace(IList<BloccoLista> destinazione, IList<BloccoLista> sorgente)
    {
        for (int i = 0; i < destinazione.Count; i++)
        {
            switch (destinazione[i], sorgente[i])
            {
                case (BloccoSingolo sd, BloccoSingolo ss):
                    sd.Esercizio.Stato = ss.Esercizio.Stato;
                    break;
                case (BloccoGruppo gd, BloccoGruppo gs):
                    for (int j = 0; j < gd.Membri.Count; j++)
                        gd.Membri[j].Stato = gs.Membri[j].Stato;
                    break;
            }
        }
    }

    [RelayCommand]
    private async Task IniziaSessioneAsync()
    {
        _sessioneCorrente = await sessioneService.IniziaSessioneAsync(_giornoId);
        HasSessioneAttiva = true;
    }

    [RelayCommand]
    private async Task SelectEsercizioAsync(EsercizioConTarget item)
    {
        if (_sessioneCorrente is null)
        {
            _sessioneCorrente = await sessioneService.IniziaSessioneAsync(_giornoId);
            HasSessioneAttiva = true;
        }

        await Shell.Current.GoToAsync(
            $"sessione?esercizioGiornoId={item.EsercizioGiorno.Id}&sessioneId={_sessioneCorrente.Id}");
    }

    [RelayCommand]
    private async Task AggiungiEsercizioAsync()
    {
        await Shell.Current.GoToAsync($"esercizioForm?giornoId={_giornoId}");
    }

    [RelayCommand]
    private async Task ModificaEsercizioAsync(EsercizioConTarget item)
    {
        await Shell.Current.GoToAsync(
            $"esercizioForm?giornoId={_giornoId}&esercizioGiornoId={item.EsercizioGiorno.Id}");
    }

    [RelayCommand]
    private async Task EliminaEsercizioAsync(EsercizioConTarget item)
    {
        bool ok = await Shell.Current.DisplayAlertAsync(
            "Rimuovi esercizio",
            $"Rimuovere \"{item.Esercizio.Nome}\" da questo giorno?",
            "Rimuovi", "Annulla");

        if (!ok) return;
        await schedaService.DeleteEsercizioGiornoAsync(item.EsercizioGiorno.Id);
        await LoadEserciziAsync();
    }

    [RelayCommand]
    private async Task FineSessioneAsync()
    {
        if (_sessioneCorrente is null) return;

        bool conferma = await Shell.Current.DisplayAlertAsync(
            "Fine Sessione",
            "Vuoi terminare la sessione di allenamento?",
            "Termina", "Annulla");

        if (!conferma) return;

        // Archivia eventuali super set "solo per oggi" creati in questa sessione:
        // i membri perdono il GruppoId (tornano singoli per le sessioni successive),
        // il record GruppoEsercizi resta con IsArchiviato=true così lo storico continua a vederlo.
        await schedaService.ArchiviaGruppiTemporaneiAsync(_giornoId);

        await sessioneService.FineSessioneAsync(_sessioneCorrente.Id);
        _sessioneCorrente = null;
        HasSessioneAttiva = false;
        await LoadEserciziAsync();
    }

    // --- Comandi gestione gruppi (super set) ---

    [RelayCommand]
    private async Task CreaSuperSetAsync()
    {
        // Esercizi candidati: solo i singoli (non già in un gruppo)
        var candidati = Blocchi.OfType<BloccoSingolo>()
            .Select(b => b.Esercizio)
            .ToList();

        if (candidati.Count < 2)
        {
            await Shell.Current.DisplayAlertAsync(
                "Super set",
                "Servono almeno 2 esercizi singoli (non già in un gruppo) per creare un super set.",
                "OK");
            return;
        }

        // Loop di selezione: l'utente sceglie un esercizio alla volta.
        var selezionati = new List<EsercizioConTarget>();
        var rimanenti = new List<EsercizioConTarget>(candidati);

        while (rimanenti.Count > 0)
        {
            var nomi = rimanenti.Select(e => e.Esercizio.Nome).ToArray();
            string titolo = selezionati.Count == 0
                ? "Primo esercizio del super set"
                : $"Aggiungi un altro ({selezionati.Count} selezionati)";
            string? confermaOra = selezionati.Count >= 2 ? "Crea super set ora" : null;

            string scelta = await Shell.Current.DisplayActionSheetAsync(titolo, "Annulla", confermaOra, nomi);

            if (string.IsNullOrEmpty(scelta) || scelta == "Annulla")
                return;

            if (scelta == confermaOra)
                break;

            var scelto = rimanenti.FirstOrDefault(e => e.Esercizio.Nome == scelta);
            if (scelto is null) continue;
            selezionati.Add(scelto);
            rimanenti.Remove(scelto);
        }

        if (selezionati.Count < 2) return;

        // Gestione SerieTarget: se diversi, chiede all'utente quale tenere.
        int? serieNormalizzata = await NormalizzaSerieTargetAsync(selezionati);

        // Nome opzionale del gruppo
        string? nome = await Shell.Current.DisplayPromptAsync(
            "Nome super set",
            "Dai un nome al super set (opzionale)",
            accept: "Crea",
            cancel: "Senza nome",
            placeholder: "Es. Push, Antagonisti...",
            maxLength: 100);

        // Se siamo in una sessione attiva, chiediamo se questo super set è solo per oggi
        // (effimero, auto-archiviato a FineSessione) o salvato nella scheda (persistente).
        bool isTemporaneo = false;
        if (_sessioneCorrente is not null)
        {
            string scelta = await Shell.Current.DisplayActionSheetAsync(
                "Dove salvare questo super set?",
                "Annulla",
                null,
                "Salva nella scheda",
                "Solo per questa sessione");

            if (string.IsNullOrEmpty(scelta) || scelta == "Annulla") return;
            isTemporaneo = scelta == "Solo per questa sessione";
        }

        await schedaService.CreaGruppoAsync(
            _giornoId,
            selezionati.Select(e => e.EsercizioGiorno.Id).ToList(),
            string.IsNullOrWhiteSpace(nome) ? null : nome.Trim(),
            serieNormalizzata,
            isTemporaneo);

        await LoadEserciziAsync();
    }

    [RelayCommand]
    private async Task GestisciGruppoAsync(BloccoGruppo blocco)
    {
        // Candidati per "aggiungi": singoli del giorno
        var puoAggiungere = Blocchi.OfType<BloccoSingolo>().Any();

        var azioni = new List<string> { "Sciogli super set", "Rinomina" };
        if (puoAggiungere) azioni.Insert(0, "Aggiungi esercizio");

        string scelta = await Shell.Current.DisplayActionSheetAsync(
            blocco.DisplayName, "Annulla", null, [.. azioni]);

        if (string.IsNullOrEmpty(scelta) || scelta == "Annulla") return;

        switch (scelta)
        {
            case "Aggiungi esercizio":
                await AggiungiEsercizioAGruppoAsync(blocco);
                break;
            case "Sciogli super set":
                await SciogliGruppoAsync(blocco);
                break;
            case "Rinomina":
                await RinominaGruppoAsync(blocco);
                break;
        }
    }

    [RelayCommand]
    private async Task SciogliGruppoAsync(BloccoGruppo blocco)
    {
        bool ok = await Shell.Current.DisplayAlertAsync(
            "Sciogli super set",
            $"Sciogliere \"{blocco.DisplayName}\"? Gli esercizi torneranno singoli.",
            "Sciogli", "Annulla");
        if (!ok) return;

        await schedaService.SciogliGruppoAsync(blocco.Gruppo.Id);
        await LoadEserciziAsync();
    }

    [RelayCommand]
    private async Task RinominaGruppoAsync(BloccoGruppo blocco)
    {
        string? nome = await Shell.Current.DisplayPromptAsync(
            "Rinomina super set",
            "Nuovo nome (lascia vuoto per rimuovere)",
            accept: "Salva",
            cancel: "Annulla",
            placeholder: "Es. Push, Antagonisti...",
            initialValue: blocco.Gruppo.Nome ?? "",
            maxLength: 100);

        if (nome is null) return; // Annulla

        await schedaService.RinominaGruppoAsync(blocco.Gruppo.Id, string.IsNullOrWhiteSpace(nome) ? null : nome.Trim());
        await LoadEserciziAsync();
    }

    [RelayCommand]
    private async Task RimuoviDaGruppoAsync(EsercizioConTarget item)
    {
        if (!item.EsercizioGiorno.GruppoId.HasValue) return;
        bool ok = await Shell.Current.DisplayAlertAsync(
            "Esci dal gruppo",
            $"Rimuovere \"{item.Esercizio.Nome}\" dal super set? L'esercizio resta nel giorno come singolo.",
            "Rimuovi", "Annulla");
        if (!ok) return;

        await schedaService.RimuoviEsercizioDaGruppoAsync(item.EsercizioGiorno.Id);
        await LoadEserciziAsync();
    }

    private async Task AggiungiEsercizioAGruppoAsync(BloccoGruppo blocco)
    {
        var candidati = Blocchi.OfType<BloccoSingolo>()
            .Select(b => b.Esercizio)
            .ToList();
        if (candidati.Count == 0) return;

        var nomi = candidati.Select(e => e.Esercizio.Nome).ToArray();
        string scelta = await Shell.Current.DisplayActionSheetAsync(
            $"Aggiungi a \"{blocco.DisplayName}\"", "Annulla", null, nomi);

        if (string.IsNullOrEmpty(scelta) || scelta == "Annulla") return;

        var scelto = candidati.FirstOrDefault(e => e.Esercizio.Nome == scelta);
        if (scelto is null) return;

        // Normalizza SerieTarget tra membri esistenti + nuovo
        var insieme = new List<EsercizioConTarget>(blocco.Membri) { scelto };
        int? serieNormalizzata = await NormalizzaSerieTargetAsync(insieme);

        await schedaService.AggiungiEsercizioAGruppoAsync(blocco.Gruppo.Id, scelto.EsercizioGiorno.Id, serieNormalizzata);
        await LoadEserciziAsync();
    }

    /// <summary>
    /// Se gli esercizi hanno SerieTarget diversi, chiede all'utente quale valore comune
    /// applicare a tutti. Se sono già uguali (incluso null) restituisce null = "non toccare".
    /// </summary>
    private static async Task<int?> NormalizzaSerieTargetAsync(IList<EsercizioConTarget> membri)
    {
        var valori = membri.Select(m => m.EsercizioGiorno.SerieTarget).Distinct().ToList();
        if (valori.Count <= 1) return null; // Tutti uguali (incluso tutti null): nessuna normalizzazione

        var nonNulli = valori.Where(v => v.HasValue).Select(v => v!.Value).OrderBy(v => v).ToList();
        if (nonNulli.Count == 0) return null;

        var opzioni = nonNulli.Select(n => $"{n} serie").ToArray();
        string scelta = await Shell.Current.DisplayActionSheetAsync(
            "I set sono uguali per ogni esercizio del super set. Scegli quante serie",
            "Annulla", null, opzioni);

        if (string.IsNullOrEmpty(scelta) || scelta == "Annulla") return null;

        var idx = Array.IndexOf(opzioni, scelta);
        return idx >= 0 ? nonNulli[idx] : null;
    }
}
