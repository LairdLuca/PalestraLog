using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PalestraLog.Models;
using PalestraLog.Models.Enums;
using PalestraLog.Services.Interfaces;

namespace PalestraLog.ViewModels;

/// <summary>
/// Dettaglio di un esercizio nella sessione storica: include i set effettuati,
/// la fatica registrata, e helper per la formattazione (con varianti per hold).
/// </summary>
public class StoricoEsercizioDettaglio
{
    public Esercizio Esercizio { get; set; } = null!;
    public EsercizioSessione EsercizioSessione { get; set; } = null!;
    public List<SetRecord> Sets { get; set; } = [];

    public string Nome => Esercizio.Nome;
    public bool IsCardio => Esercizio.TipoEsercizio == TipoEsercizio.Cardio;

    /// <summary>
    /// Hold = corpo libero senza ripetizioni ma con durata dichiarata. Determinato dai set
    /// effettivamente registrati, non dal target di scheda (lo storico è autoritativo).
    /// </summary>
    public bool IsHold =>
        Esercizio.TipoEsercizio == TipoEsercizio.CorpoLibero
        && Sets.Any(s => !s.IsRiscaldamento && s.DurataCardioSecondi.HasValue);

    public string FaticaLabel
    {
        get
        {
            if (EsercizioSessione.Fatica <= 0) return "";
            return EsercizioSessione.Fatica switch
            {
                1 => "TROPPO LEGGERO",
                2 => "GIUSTO",
                3 => "MEDIO",
                4 => "PESANTE",
                5 => "AL LIMITE",
                _ => ""
            };
        }
    }

    public Color FaticaColore => EsercizioSessione.Fatica switch
    {
        1 => Color.FromArgb("#10B981"),
        2 => Color.FromArgb("#84CC16"),
        3 => Color.FromArgb("#FFB800"),
        4 => Color.FromArgb("#F97316"),
        5 => Color.FromArgb("#FF3B30"),
        _ => Colors.Gray
    };

    public bool HasFatica => EsercizioSessione.Fatica > 0;

    /// <summary>
    /// Set di lavoro (esclusi warmup), ordinati come arrivati.
    /// </summary>
    public IList<SetRecord> SetLavoro =>
        Sets.Where(s => !s.IsRiscaldamento).OrderBy(s => s.NumeroSet).ToList();

    /// <summary>
    /// Set formattati come singola stringa multilinea (una riga per set, numerata).
    /// Più semplice per il binding XAML rispetto a una CollectionView annidata.
    /// </summary>
    public string RigheSetMultilinea
    {
        get
        {
            var righe = SetLavoro.Select((s, i) => $"{i + 1})  {FormattaSet(s)}");
            return string.Join("\n", righe);
        }
    }

    public bool HasSetLavoro => SetLavoro.Count > 0;

    /// <summary>
    /// Sintesi di un set in una riga (formattazione dipende da tipo: pesi/hold/cardio).
    /// </summary>
    public string FormattaSet(SetRecord s)
    {
        if (IsHold)
        {
            var peso = s.Peso > 0 ? $"{s.Peso:F1}kg · " : "";
            var sec = s.DurataCardioSecondi ?? 0;
            return $"{peso}{sec}s";
        }
        if (IsCardio)
        {
            var parti = new List<string>();
            if (s.Velocita.HasValue) parti.Add($"{s.Velocita:F1}km/h");
            if (s.Inclinazione.HasValue) parti.Add($"{s.Inclinazione:F1}%");
            if (s.Distanza.HasValue) parti.Add($"{s.Distanza:F2}km");
            if (s.DurataCardioSecondi.HasValue) parti.Add($"{TimeSpan.FromSeconds(s.DurataCardioSecondi.Value):mm\\:ss}");
            return parti.Count > 0 ? string.Join(" · ", parti) : "—";
        }
        // Pesi / corpo libero a reps
        var p = s.Peso.HasValue ? $"{s.Peso:F1}kg" : "—";
        var r = s.Ripetizioni.HasValue ? $"{s.Ripetizioni}" : "—";
        return $"{p} x {r}";
    }
}

public abstract partial class BloccoStorico : ObservableObject { }

public partial class BloccoStoricoSingolo : BloccoStorico
{
    public StoricoEsercizioDettaglio Esercizio { get; set; } = null!;
}

public partial class BloccoStoricoGruppo : BloccoStorico
{
    public GruppoEsercizi? Gruppo { get; set; }
    public List<StoricoEsercizioDettaglio> Membri { get; set; } = [];

    public string LabelTipo => Membri.Count switch
    {
        2 => "SUPER SET",
        3 => "TRI-SET",
        >= 4 => "GIANT SET",
        _ => ""
    };

    /// <summary>
    /// Nome del gruppo se ancora esistente nella scheda; "SUPER SET · (sciolto)" altrimenti.
    /// </summary>
    public string DisplayName
    {
        get
        {
            if (Gruppo is null) return $"{LabelTipo} · (sciolto)";
            return string.IsNullOrWhiteSpace(Gruppo.Nome) ? LabelTipo : $"{LabelTipo} · {Gruppo.Nome}";
        }
    }
}

public partial class StoricoSessioneDettaglioViewModel(
    ISessioneService sessioneService,
    ISchedaService schedaService,
    IDatabaseService databaseService) : ObservableObject, IQueryAttributable
{
    private int _sessioneId;

    [ObservableProperty] private string _giornoNome = string.Empty;
    [ObservableProperty] private string _dataFormattata = string.Empty;
    [ObservableProperty] private string _durataFormattata = string.Empty;
    [ObservableProperty] private ObservableCollection<BloccoStorico> _blocchi = [];
    [ObservableProperty] private bool _isBusy;

    /// <summary>
    /// Testo dell'EmptyView. Una lista vuota non deve dire "nessun esercizio" quando la
    /// sessione non esiste o il caricamento è fallito: sarebbe un'affermazione falsa sui dati.
    /// </summary>
    [ObservableProperty] private string _messaggioVuoto = "Nessun esercizio in questa sessione";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("sessioneId", out var sid) && int.TryParse(sid?.ToString(), out var id))
            _sessioneId = id;
        LoadCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;
        if (_sessioneId <= 0)
        {
            MessaggioVuoto = "Sessione non trovata";
            return;
        }
        IsBusy = true;
        try
        {
            await databaseService.InitializeAsync();

            var sessione = await databaseService.Database.Table<Sessione>()
                .Where(s => s.Id == _sessioneId)
                .FirstOrDefaultAsync();
            if (sessione is null)
            {
                MessaggioVuoto = "Sessione non trovata";
                return;
            }

            var giorno = await databaseService.Database.Table<Giorno>()
                .Where(g => g.Id == sessione.GiornoId)
                .FirstOrDefaultAsync();
            GiornoNome = giorno?.Nome ?? "Sconosciuto";
            DataFormattata = sessione.DataInizio.ToString("dd/MM/yyyy HH:mm");
            var ts = TimeSpan.FromSeconds(sessione.DurataTotaleSecondi);
            DurataFormattata = ts.Hours > 0 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"mm\:ss");

            // Carica EsercizioSessione (già ordinati per Ordine grazie al fix iter 2).
            var esSessione = await sessioneService.GetEserciziSessioneAsync(_sessioneId);

            // Costruisci dettaglio per ogni EsercizioSessione (set + esercizio).
            var dettagli = new List<StoricoEsercizioDettaglio>();
            foreach (var es in esSessione)
            {
                var esercizio = await schedaService.GetEsercizioAsync(es.EsercizioId);
                if (esercizio is null) continue;
                var sets = await sessioneService.GetSetRecordsAsync(es.Id);
                dettagli.Add(new StoricoEsercizioDettaglio
                {
                    Esercizio = esercizio,
                    EsercizioSessione = es,
                    Sets = sets
                });
            }

            // Carica i gruppi referenziati (per nome). Quelli con FK orfana (gruppo eliminato
            // dopo l'esecuzione) sono gestiti da DisplayName con fallback "(sciolto)".
            var gruppiIds = dettagli
                .Where(d => d.EsercizioSessione.GruppoEserciziId.HasValue)
                .Select(d => d.EsercizioSessione.GruppoEserciziId!.Value)
                .Distinct()
                .ToList();
            var gruppiById = new Dictionary<int, GruppoEsercizi>();
            foreach (var gid in gruppiIds)
            {
                var g = await databaseService.Database.Table<GruppoEsercizi>()
                    .Where(x => x.Id == gid)
                    .FirstOrDefaultAsync();
                if (g is not null) gruppiById[gid] = g;
            }

            // Costruisci blocchi: dettagli consecutivi con stesso GruppoEserciziId vanno
            // nello stesso BloccoStoricoGruppo, gli altri in BloccoStoricoSingolo.
            var nuoviBlocchi = new List<BloccoStorico>();
            var gruppiInseriti = new HashSet<int>();
            foreach (var d in dettagli)
            {
                if (d.EsercizioSessione.GruppoEserciziId is int gId)
                {
                    if (gruppiInseriti.Contains(gId)) continue;
                    var membri = dettagli.Where(x => x.EsercizioSessione.GruppoEserciziId == gId).ToList();
                    if (membri.Count >= 2)
                    {
                        gruppiById.TryGetValue(gId, out var gruppo);
                        nuoviBlocchi.Add(new BloccoStoricoGruppo { Gruppo = gruppo, Membri = membri });
                        gruppiInseriti.Add(gId);
                    }
                    else
                    {
                        nuoviBlocchi.Add(new BloccoStoricoSingolo { Esercizio = d });
                    }
                }
                else
                {
                    nuoviBlocchi.Add(new BloccoStoricoSingolo { Esercizio = d });
                }
            }

            Blocchi = new ObservableCollection<BloccoStorico>(nuoviBlocchi);
        }
        catch (Exception ex)
        {
            // Il comando è invocato senza await da ApplyQueryAttributes: senza questo catch
            // l'eccezione risalirebbe al thread UI e chiuderebbe l'app. La pagina resta
            // disegnata e vuota, e l'EmptyView dichiara il guasto invece di "nessun esercizio".
            MessaggioVuoto = "Impossibile caricare questa sessione";
            System.Diagnostics.Debug.WriteLine($"[StoricoSessioneDettaglio] Load fallito: {ex}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
