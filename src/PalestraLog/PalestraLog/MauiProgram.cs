using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using PalestraLog.Services;
using PalestraLog.Services.Interfaces;
using PalestraLog.ViewModels;
using PalestraLog.Views;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace PalestraLog;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services - Singletons
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
        builder.Services.AddSingleton<ISchedaService, SchedaService>();
        builder.Services.AddSingleton<ISessioneService, SessioneService>();
        builder.Services.AddSingleton<IStatisticheService, StatisticheService>();
        // Backup: il comando di export è provvisoriamente sulla Home (vedi HomeViewModel).
        builder.Services.AddSingleton<IBackupService, BackupService>();
        // Singleton: lo stesso semaforo serializza export e pulizia all'avvio della cartella transitoria.
        builder.Services.AddSingleton<IExportService, ExportService>();

        // Timer - Singleton: il timer continua a girare anche quando l'utente
        // esce dalla SessionePage (la pagina viene smontata, il timer no).
        builder.Services.AddSingleton<ITimerService, TimerService>();

        // ViewModels
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<GiornoListViewModel>();
        builder.Services.AddTransient<EsercizioListViewModel>();
        // Singleton: la SessionePage può essere ricreata, ma lo stato (set inseriti,
        // peso/reps correnti, modalità warmup) deve sopravvivere al pop della pagina.
        builder.Services.AddSingleton<SessioneViewModel>();
        builder.Services.AddTransient<SchedaFormViewModel>();
        builder.Services.AddTransient<EsercizioFormViewModel>();
        builder.Services.AddTransient<StoricoViewModel>();
        builder.Services.AddTransient<StoricoSessioneDettaglioViewModel>();

        // Pages
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<GiornoListPage>();
        builder.Services.AddTransient<EsercizioListPage>();
        builder.Services.AddTransient<SessionePage>();
        builder.Services.AddTransient<SchedaFormPage>();
        builder.Services.AddTransient<EsercizioFormPage>();
        builder.Services.AddTransient<StoricoPage>();
        builder.Services.AddTransient<StoricoSessioneDettaglioPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
