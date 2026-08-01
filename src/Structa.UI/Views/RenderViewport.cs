using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using Serilog;
using Silk.NET.OpenGL;
using Structa.Rendering;
using GlCamera = Structa.Camera.Camera3D;

namespace Structa.UI.Views;

/// <summary>
/// Superfície de renderização 3D: integra o contexto OpenGL do Avalonia (<see cref="OpenGlControlBase"/>)
/// com as chamadas GL fortemente tipadas do Silk.NET, executando o <see cref="RenderEngine"/> em loop
/// contínuo alinhado ao vsync via <see cref="OpenGlControlBase.RequestNextFrameRendering"/>.
/// </summary>
public sealed class RenderViewport : OpenGlControlBase
{
    public static readonly StyledProperty<double> FramesPerSecondProperty =
        AvaloniaProperty.Register<RenderViewport, double>(nameof(FramesPerSecond));

    public double FramesPerSecond
    {
        get => GetValue(FramesPerSecondProperty);
        private set => SetValue(FramesPerSecondProperty, value);
    }

    private readonly GlCamera _camera = new();
    private readonly RenderEngine _engine = new();
    private GL? _gl;
    private long _lastTimestamp;

    protected override void OnOpenGlInit(GlInterface gl)
    {
        base.OnOpenGlInit(gl);

        Log.Information("OpenGL contexto: {Version} (perfil={Profile})", gl.Version, GlVersion.Type);

        try
        {
            _gl = GL.GetApi(gl.GetProcAddress);
            _engine.Initialize(_gl);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Falha ao inicializar o RenderEngine");
            throw;
        }

        _lastTimestamp = Stopwatch.GetTimestamp();

        RequestNextFrameRendering();
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        try
        {
            var now = Stopwatch.GetTimestamp();
            var deltaSeconds = Stopwatch.GetElapsedTime(_lastTimestamp, now).TotalSeconds;
            _lastTimestamp = now;

            var scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
            var pixelWidth = Math.Max(1, (uint)(Bounds.Width * scaling));
            var pixelHeight = Math.Max(1, (uint)(Bounds.Height * scaling));

            _camera.AspectRatio = (float)pixelWidth / pixelHeight;

            // Avalonia espera que o conteúdo seja desenhado no framebuffer indicado por "fb"
            // (não necessariamente o 0/padrão), que ela compõe no restante da árvore visual.
            _gl!.BindFramebuffer(GLEnum.Framebuffer, (uint)fb);

            _engine.Resize(pixelWidth, pixelHeight);
            _engine.Render(_camera.GetViewMatrix(), _camera.GetProjectionMatrix(), _camera.Position, deltaSeconds);

            var fps = Math.Round(_engine.Stats.FramesPerSecond);
            Dispatcher.UIThread.Post(() => FramesPerSecond = fps);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Falha ao renderizar o frame");
            throw;
        }
        finally
        {
            RequestNextFrameRendering();
        }
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        _engine.Dispose();
        base.OnOpenGlDeinit(gl);
    }
}
