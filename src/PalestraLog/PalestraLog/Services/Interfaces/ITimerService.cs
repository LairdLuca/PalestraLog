namespace PalestraLog.Services.Interfaces;

public interface ITimerService
{
    TimeSpan Elapsed { get; }
    TimeSpan LapElapsed { get; }
    bool IsRunning { get; }

    event EventHandler<TimeSpan>? Tick;
    event EventHandler? CountdownFinished;

    void Start();
    void Stop();
    TimeSpan Lap();
    void Reset();

    /// <summary>
    /// Riprende il timer come se fosse stato attivo per `elapsed` (totale) e `lapElapsed`
    /// (set corrente). Usato quando si rientra in un esercizio già iniziato per
    /// recuperare il tempo trascorso fuori dalla pagina.
    /// </summary>
    void Resume(TimeSpan elapsed, TimeSpan lapElapsed);

    void StartCountdown(TimeSpan duration);
}
