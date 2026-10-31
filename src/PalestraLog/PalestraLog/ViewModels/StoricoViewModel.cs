using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PalestraLog.Models;
using PalestraLog.Services.Interfaces;

namespace PalestraLog.ViewModels;

public class SessioneRiepilogo
{
    public Sessione Sessione { get; set; } = null!;
    public string GiornoNome { get; set; } = string.Empty;
    public string DataFormattata => Sessione.DataInizio.ToString("dd/MM/yyyy HH:mm");
    public string DurataFormattata
    {
        get
        {
            var ts = TimeSpan.FromSeconds(Sessione.DurataTotaleSecondi);
            return ts.Hours > 0 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"mm\:ss");
        }
    }
}

public partial class StoricoViewModel(
    IDatabaseService databaseService,
    ISessioneService sessioneService) : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<SessioneRiepilogo> _sessioni = [];

    [ObservableProperty]
    private bool _isBusy;

    [RelayCommand]
    private async Task LoadStoricoAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await databaseService.InitializeAsync();

            var tutteLeSessoni = await databaseService.Database
                .Table<Sessione>()
                .Where(s => s.DataFine != null)
                .OrderByDescending(s => s.DataInizio)
                .Take(50)
                .ToListAsync();

            var lista = new List<SessioneRiepilogo>();
            foreach (var s in tutteLeSessoni)
            {
                var giorno = await databaseService.Database
                    .Table<Giorno>()
                    .Where(g => g.Id == s.GiornoId)
                    .FirstOrDefaultAsync();

                lista.Add(new SessioneRiepilogo
                {
                    Sessione = s,
                    GiornoNome = giorno?.Nome ?? "Sconosciuto"
                });
            }

            Sessioni = new ObservableCollection<SessioneRiepilogo>(lista);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ApriDettaglioAsync(SessioneRiepilogo? riepilogo)
    {
        if (riepilogo is null) return;
        await Shell.Current.GoToAsync($"storicoSessioneDettaglio?sessioneId={riepilogo.Sessione.Id}");
    }

    [RelayCommand]
    private async Task EliminaSessioneAsync(SessioneRiepilogo? riepilogo)
    {
        if (riepilogo is null) return;

        var conferma = await Shell.Current.DisplayAlertAsync(
            "Eliminare sessione?",
            $"{riepilogo.GiornoNome} del {riepilogo.DataFormattata}\n\nL'azione non può essere annullata.",
            "Elimina",
            "Annulla");

        if (!conferma) return;

        await sessioneService.EliminaSessioneAsync(riepilogo.Sessione.Id);
        Sessioni.Remove(riepilogo);
    }
}
