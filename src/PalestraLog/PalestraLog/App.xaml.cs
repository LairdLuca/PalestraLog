using Microsoft.Extensions.DependencyInjection;
using PalestraLog.Services.Interfaces;

namespace PalestraLog
{
    public partial class App : Application
    {
        private readonly IExportService _exportService;

        public App(IExportService exportService)
        {
            InitializeComponent();
            _exportService = exportService;

            // Catch globale per eccezioni che altrimenti farebbero crash
            // silenzioso dell'app quando gira senza debugger collegato.
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    System.Diagnostics.Debug.WriteLine($"[UnhandledException] {ex}");
            };
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[UnobservedTaskException] {e.Exception}");
                e.SetObserved();
            };
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // I file dell'ultimo export restano nella cache finché l'app che li riceve può ancora
            // leggerli: si rimuovono qui e all'inizio dell'export successivo (AD-16). Non nel
            // costruttore: Android può riavviare il processo solo per servire i file a quell'app,
            // senza aprire alcuna finestra, e la pulizia li cancellerebbe mentre li legge.
            _ = Task.Run(_exportService.ClearTransientFilesAsync);

            return new Window(new AppShell());
        }
    }
}