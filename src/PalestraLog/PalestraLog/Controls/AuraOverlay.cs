using System.Diagnostics;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace PalestraLog.Controls;

/// <summary>
/// Aura ambientale full-screen disegnata via SkiaSharp.
/// Tre layer: glow morbido sui bordi (gradiente sfumato verso il centro),
/// pulsazione di intensità ciclica, particelle che fluttuano dai bordi.
/// Il colore e l'intensità derivano da <see cref="AuraState"/>.
/// </summary>
public class AuraOverlay : SKCanvasView
{
    public static readonly BindableProperty AuraStateProperty = BindableProperty.Create(
        nameof(AuraState),
        typeof(AuraState),
        typeof(AuraOverlay),
        AuraState.Idle,
        propertyChanged: OnAuraStateChanged);

    public AuraState AuraState
    {
        get => (AuraState)GetValue(AuraStateProperty);
        set => SetValue(AuraStateProperty, value);
    }

    private readonly Stopwatch _clock = new();
    private IDispatcherTimer? _timer;
    private readonly List<Particle> _particles = new(64);
    private readonly Random _rng = new();
    private float _lastSpawnT;

    private const int TargetFps = 30;
    private const int MaxParticles = 40;
    private const float ParticleMaxAgeSec = 2.5f;

    public AuraOverlay()
    {
        EnableTouchEvents = false;
        IgnorePixelScaling = false;
        PaintSurface += OnPaintSurface;
        HandlerChanged += OnHandlerChangedInternal;
    }

    private static void OnAuraStateChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AuraOverlay aura)
            aura.UpdateRunning();
    }

    private void OnHandlerChangedInternal(object? sender, EventArgs e)
    {
        if (Handler is null)
        {
            // Detached: ferma tutto e libera il timer.
            StopTimer();
            if (_timer is not null)
            {
                _timer.Tick -= OnTick;
                _timer = null;
            }
            _particles.Clear();
            return;
        }

        // Attached: prepara il timer usando il dispatcher del control (sempre valido).
        var dispatcher = this.Dispatcher ?? Application.Current?.Dispatcher;
        if (dispatcher is null) return;

        _timer ??= dispatcher.CreateTimer();
        if (_timer is null) return;

        _timer.Interval = TimeSpan.FromMilliseconds(1000.0 / TargetFps);
        _timer.Tick -= OnTick;
        _timer.Tick += OnTick;
        UpdateRunning();
    }

    private void UpdateRunning()
    {
        if (_timer is null) return;

        if (AuraState == AuraState.Idle)
        {
            StopTimer();
            _particles.Clear();
            InvalidateSurface(); // un ultimo redraw per ripulire lo schermo
            return;
        }

        if (!_timer.IsRunning)
        {
            _clock.Restart();
            _lastSpawnT = 0;
            _timer.Start();
        }
    }

    private void StopTimer()
    {
        if (_timer is { IsRunning: true })
            _timer.Stop();
        _clock.Stop();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        // Se il control è stato smontato (Handler null) il timer non deve più
        // chiamare InvalidateSurface — fermo subito.
        if (Handler is null)
        {
            StopTimer();
            return;
        }
        try
        {
            InvalidateSurface();
        }
        catch
        {
            // Render loop non deve mai propagare eccezioni: l'aura è decorativa,
            // un crash qui ucciderebbe l'intera app.
            StopTimer();
        }
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        try
        {
            var canvas = e.Surface.Canvas;
            var info = e.Info;
            canvas.Clear(SKColors.Transparent);

            if (AuraState == AuraState.Idle) return;
            if (info.Width <= 0 || info.Height <= 0) return;

            var t = (float)_clock.Elapsed.TotalSeconds;

            // Configurazione specifica per stato.
            var (baseColor, periodSec, baseIntensity, spawnRatePerSec) = AuraState switch
            {
                AuraState.Warmup => (new SKColor(0x10, 0xB9, 0x81), 1.6f, 0.50f, 8f),
                AuraState.SetRunning => (new SKColor(0xFF, 0x3B, 0x30), 0.9f, 1.00f, 18f),
                _ => (SKColors.Transparent, 1f, 0f, 0f)
            };

            // Pulsazione SinInOut tra 0.55 e 1.0 della baseIntensity.
            var phase = (t % periodSec) / periodSec; // 0..1
            var sin = (float)((Math.Sin(phase * Math.PI * 2 - Math.PI / 2) + 1) / 2); // 0..1
            var pulse = 0.55f + 0.45f * sin;
            var intensity = baseIntensity * pulse;

            DrawGlow(canvas, info, baseColor, intensity);

            UpdateAndDrawParticles(canvas, info, baseColor, t, spawnRatePerSec);
        }
        catch
        {
            // Vedi commento in OnTick: niente exception fuori dal render dell'aura.
            StopTimer();
        }
    }

    private static void DrawGlow(SKCanvas canvas, SKImageInfo info, SKColor color, float intensity)
    {
        // Quattro gradienti lineari (uno per lato) che sfumano da colore opaco al centro trasparente.
        // Lo spessore "morbido" del glow è circa 35% della dimensione minore.
        float w = info.Width;
        float h = info.Height;
        float thickness = MathF.Min(w, h) * 0.35f;

        byte alpha = (byte)Math.Clamp(intensity * 180, 0, 255);
        var edge = new SKColor(color.Red, color.Green, color.Blue, alpha);
        var inner = new SKColor(color.Red, color.Green, color.Blue, 0);

        // Top
        DrawEdgeGlow(canvas, new SKRect(0, 0, w, thickness),
            new SKPoint(0, 0), new SKPoint(0, thickness), edge, inner);
        // Bottom
        DrawEdgeGlow(canvas, new SKRect(0, h - thickness, w, h),
            new SKPoint(0, h), new SKPoint(0, h - thickness), edge, inner);
        // Left
        DrawEdgeGlow(canvas, new SKRect(0, 0, thickness, h),
            new SKPoint(0, 0), new SKPoint(thickness, 0), edge, inner);
        // Right
        DrawEdgeGlow(canvas, new SKRect(w - thickness, 0, w, h),
            new SKPoint(w, 0), new SKPoint(w - thickness, 0), edge, inner);
    }

    private static void DrawEdgeGlow(SKCanvas canvas, SKRect rect, SKPoint from, SKPoint to, SKColor edge, SKColor inner)
    {
        using var shader = SKShader.CreateLinearGradient(
            from, to,
            new[] { edge, inner },
            new[] { 0f, 1f },
            SKShaderTileMode.Clamp);
        using var paint = new SKPaint
        {
            Shader = shader,
            IsAntialias = true,
            BlendMode = SKBlendMode.Plus
        };
        canvas.DrawRect(rect, paint);
    }

    private void UpdateAndDrawParticles(SKCanvas canvas, SKImageInfo info, SKColor color, float now, float spawnRate)
    {
        // Spawn proporzionale al tempo trascorso dall'ultimo tick.
        float dt = MathF.Max(1f / TargetFps, now - _lastSpawnT);
        // Numero di particelle da generare in questo frame.
        float spawnFloat = dt * spawnRate;
        int spawnInt = (int)spawnFloat;
        // Frazione probabilistica: se spawnFloat = 1.7 → 1 sicuro + 0.7 di prob 1 in più.
        if (_rng.NextSingle() < spawnFloat - spawnInt) spawnInt++;
        for (int i = 0; i < spawnInt && _particles.Count < MaxParticles; i++)
            SpawnParticle(info);
        _lastSpawnT = now;

        // Update + draw.
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Age += 1f / TargetFps;
            if (p.Age >= p.MaxAge)
            {
                _particles.RemoveAt(i);
                continue;
            }
            p.X += p.Vx / TargetFps;
            p.Y += p.Vy / TargetFps;
            _particles[i] = p;

            DrawParticle(canvas, p, color);
        }
    }

    private void SpawnParticle(SKImageInfo info)
    {
        // Spawna sul bordo (top/bottom/left/right) con drift verso il centro.
        int edge = _rng.Next(4);
        float w = info.Width, h = info.Height;
        float x, y, vx, vy;
        float driftStrength = 18f; // px/sec verso l'interno
        float lateral = (_rng.NextSingle() - 0.5f) * 12f;

        switch (edge)
        {
            case 0: x = _rng.NextSingle() * w; y = 0; vx = lateral; vy = driftStrength; break;
            case 1: x = _rng.NextSingle() * w; y = h; vx = lateral; vy = -driftStrength; break;
            case 2: x = 0; y = _rng.NextSingle() * h; vx = driftStrength; vy = lateral; break;
            default: x = w; y = _rng.NextSingle() * h; vx = -driftStrength; vy = lateral; break;
        }

        _particles.Add(new Particle
        {
            X = x,
            Y = y,
            Vx = vx,
            Vy = vy,
            Radius = 8f + _rng.NextSingle() * 18f,
            Age = 0,
            MaxAge = ParticleMaxAgeSec * (0.7f + _rng.NextSingle() * 0.6f)
        });
    }

    private static void DrawParticle(SKCanvas canvas, Particle p, SKColor color)
    {
        // Fade-in/out simmetrico: 0 → max → 0 sulla durata.
        float life = p.Age / p.MaxAge; // 0..1
        float fade = MathF.Sin(life * MathF.PI); // 0..1..0
        byte alpha = (byte)Math.Clamp(fade * 200, 0, 255);

        var center = new SKColor(color.Red, color.Green, color.Blue, alpha);
        var outer = new SKColor(color.Red, color.Green, color.Blue, 0);

        using var shader = SKShader.CreateRadialGradient(
            new SKPoint(p.X, p.Y),
            p.Radius,
            new[] { center, outer },
            new[] { 0f, 1f },
            SKShaderTileMode.Clamp);
        using var paint = new SKPaint
        {
            Shader = shader,
            IsAntialias = true,
            BlendMode = SKBlendMode.Plus
        };
        canvas.DrawCircle(p.X, p.Y, p.Radius, paint);
    }

    private struct Particle
    {
        public float X, Y;
        public float Vx, Vy;
        public float Radius;
        public float Age;
        public float MaxAge;
    }
}
