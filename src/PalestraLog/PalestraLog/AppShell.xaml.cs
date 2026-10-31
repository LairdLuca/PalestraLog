using PalestraLog.Views;

namespace PalestraLog;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Registra le route per la navigazione
        Routing.RegisterRoute("giornoList", typeof(GiornoListPage));
        Routing.RegisterRoute("esercizioList", typeof(EsercizioListPage));
        Routing.RegisterRoute("sessione", typeof(SessionePage));
        Routing.RegisterRoute("schedaForm", typeof(SchedaFormPage));
        Routing.RegisterRoute("esercizioForm", typeof(EsercizioFormPage));
        Routing.RegisterRoute("storicoSessioneDettaglio", typeof(StoricoSessioneDettaglioPage));
    }
}
