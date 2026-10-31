using System.Diagnostics;
using PalestraLog.Services.Interfaces;

namespace PalestraLog.Services;

public class TimerService : ITimerService, IDisposable
{
    private readonly Stopwatch _stopwatch = new();
    private readonly Stopwatch _lapStopwatch = new();
    private IDispatcherTimer? _dispatcherTimer;

    // Offset di partenza per supportare Resume: l'Elapsed effettivo è
    // _baseElapsed + _stopwatch.Elapsed. Idem per lap. Stopwatch non
    // permette di settare l'elapsed direttamente, quindi usiamo un offset.
    private TimeSpan _baseElapsed = TimeSpan.Zero;
    private TimeSpan _baseLapElapsed = TimeSpan.Zero;

    private bool _isCountdown;
    private TimeSpan _countdownDuration;

    public TimeSpan Elapsed => _baseElapsed + _stopwatch.Elapsed;
    public TimeSpan LapElapsed => _baseLapElapsed + _lapStopwatch.Elapsed;
    public bool IsRunning => _stopwatch.IsRunning;

    public event EventHandler<TimeSpan>? Tick;
    public event EventHandler? CountdownFinished;

    public void Start()
    {
        _isCountdown = false;
        _stopwatch.Start();
        _lapStopwatch.Start();
        StartTickTimer();
    }

    public void Stop()
    {
        _stopwatch.Stop();
        _lapStopwatch.Stop();
        StopTickTimer();
    }

    public TimeSpan Lap()
    {
        var lapTime = LapElapsed;
        _baseLapElapsed = TimeSpan.Zero;
        _lapStopwatch.Restart();
        return lapTime;
    }

    public void Reset()
    {
        _stopwatch.Reset();
        _lapStopwatch.Reset();
        _baseElapsed = TimeSpan.Zero;
        _baseLapElapsed = TimeSpan.Zero;
        _isCountdown = false;
        StopTickTimer();
    }

    public void Resume(TimeSpan elapsed, TimeSpan lapElapsed)
    {
        _isCountdown = false;
        _baseElapsed = elapsed;
        _baseLapElapsed = lapElapsed;
        _stopwatch.Restart();
        _lapStopwatch.Restart();
        StartTickTimer();
    }

    public void StartCountdown(TimeSpan duration)
    {
        Reset();
        _isCountdown = true;
        _countdownDuration = duration;
        _stopwatch.Start();
        StartTickTimer();
    }

    private void StartTickTimer()
    {
        if (_dispatcherTimer is not null) return;

        _dispatcherTimer = Application.Current?.Dispatcher.CreateTimer();
        if (_dispatcherTimer is null) return;

        _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(33);
        _dispatcherTimer.Tick += OnTick;
        _dispatcherTimer.Start();
    }

    private void StopTickTimer()
    {
        if (_dispatcherTimer is null) return;
        _dispatcherTimer.Stop();
        _dispatcherTimer.Tick -= OnTick;
        _dispatcherTimer = null;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_isCountdown)
        {
            var remaining = _countdownDuration - _stopwatch.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                Stop();
                Tick?.Invoke(this, TimeSpan.Zero);
                CountdownFinished?.Invoke(this, EventArgs.Empty);
                return;
            }
            Tick?.Invoke(this, remaining);
        }
        else
        {
            Tick?.Invoke(this, _stopwatch.Elapsed);
        }
    }

    public void Dispose()
    {
        StopTickTimer();
        GC.SuppressFinalize(this);
    }
}
