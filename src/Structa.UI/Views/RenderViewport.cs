using System.Diagnostics;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Silk.NET.OpenGL;
using Structa.Camera;
using Structa.Core.Editor;
using Structa.Core.Messaging;
using Structa.Editor;
using Structa.Editor.Tools;
using Structa.Geometry;
using Structa.Rendering;
using Structa.Rendering.Scene;
using GlCamera = Structa.Camera.Camera3D;
using SelectionManager = Structa.Selection.SelectionManager;
using SelectionMode = Structa.Core.Selection.SelectionMode;

namespace Structa.UI.Views;

/// <summary>
/// Superfície de renderização 3D: integra o contexto OpenGL do Avalonia (<see cref="OpenGlControlBase"/>)
/// com as chamadas GL fortemente tipadas do Silk.NET, executando o <see cref="RenderEngine"/> em loop
/// contínuo alinhado ao vsync via <see cref="OpenGlControlBase.RequestNextFrameRendering"/>.
///
/// O conteúdo é composto via textura de GPU compartilhada e não participa do hit-testing normal do
/// Avalonia, então a entrada de mouse/teclado é capturada por um overlay transparente em
/// <see cref="ViewportView"/>, que chama os métodos públicos de navegação e picking abaixo.
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

    // Cores de snap do preview da ferramenta Linha: eixos usam as mesmas cores do AxesRenderer (X/Y/Z),
    // ponta existente em verde-destaque e plano livre em âmbar — cada uma comunica visualmente por que
    // o ponto está grudado ali.
    private static readonly Vector3 LineDrawColor = new(1f, 1f, 1f);
    private static readonly Vector3 EndpointSnapColor = new(0.15f, 0.90f, 0.40f);
    private static readonly Vector3 PlaneSnapColor = new(0.95f, 0.75f, 0.15f);

    private readonly GlCamera _camera = new();
    private readonly OrbitCameraController _cameraController;
    private readonly RenderEngine _engine = new();
    private readonly Scene _scene = new();
    private readonly SelectionManager _selection;
    private readonly LineTool _lineTool;
    private readonly IEventAggregator _eventAggregator;
    private readonly IDisposable _toolSubscription;
    private EditorTool _activeTool = EditorTool.Select;
    private Point? _pointerPosition;
    private GL? _gl;
    private long _lastTimestamp;

    public RenderViewport()
    {
        _cameraController = new OrbitCameraController(_camera);
        _eventAggregator = App.Services.GetRequiredService<IEventAggregator>();
        _selection = new SelectionManager(_eventAggregator);
        _lineTool = new LineTool(_scene);
        _toolSubscription = _eventAggregator.Subscribe<EditorToolChangedEvent>(OnToolChanged);

        // Conteúdo de teste: ainda não há detecção de faces (Etapa 06), então estas malhas existem só
        // para validar o picking de vértice/aresta/face/objeto e dar algo para a Linha se conectar —
        // o cubo cobre um sólido convexo simples e a folha cobre um contorno côncavo com mais vértices.
        _scene.Meshes.Add(MeshPrimitives.CreateBox("Cubo de teste", new Vector3(0f, 0f, 1f), 2f));
        _scene.Meshes.Add(MeshPrimitives.CreateAcanthusLeaf("Folha de acanto de teste", new Vector3(3f, -1.2f, 0f)));
    }

    public void Orbit(double deltaXPixels, double deltaYPixels) =>
        _cameraController.Orbit((float)deltaXPixels, (float)deltaYPixels);

    public void Pan(double deltaXPixels, double deltaYPixels) =>
        _cameraController.Pan((float)deltaXPixels, (float)deltaYPixels);

    public void Zoom(double notches) => _cameraController.Zoom((float)notches);

    public void ResetView() => _cameraController.ResetView();

    /// <summary>Posição atual do cursor (DIPs, relativa a este controle), usada para o preview da ferramenta
    /// ativa. Nulo quando o cursor está fora da viewport.</summary>
    public void UpdatePointer(Point? screenPoint) => _pointerPosition = screenPoint;

    /// <summary>Clique com o botão esquerdo: delega para a ferramenta ativa (seleção ou linha).</summary>
    public void HandlePrimaryClick(Point screenPoint, bool additive)
    {
        if (_activeTool == EditorTool.Line)
        {
            var viewportSize = new Vector2((float)Bounds.Width, (float)Bounds.Height);
            var pixelPoint = new Vector2((float)screenPoint.X, (float)screenPoint.Y);

            _lineTool.Click(pixelPoint, viewportSize, _camera.Position, _camera.GetViewMatrix(), _camera.GetProjectionMatrix());
            return;
        }

        Pick(screenPoint, additive);
    }

    /// <summary>Faz picking no ponto de tela (DIPs, relativo a este controle) e atualiza a seleção.</summary>
    public void Pick(Point screenPoint, bool additive)
    {
        var viewportSize = new Vector2((float)Bounds.Width, (float)Bounds.Height);
        var pixelPoint = new Vector2((float)screenPoint.X, (float)screenPoint.Y);

        _selection.Pick(
            pixelPoint,
            viewportSize,
            _camera.Position,
            _camera.GetViewMatrix(),
            _camera.GetProjectionMatrix(),
            _scene.Meshes,
            additive);
    }

    public void ClearSelection() => _selection.Clear();

    /// <summary>Esc: cancela o segmento pendente da ferramenta ativa; sem segmento pendente, limpa a seleção.</summary>
    public void CancelActiveTool()
    {
        if (_lineTool.IsDrawing)
        {
            _lineTool.Cancel();
            return;
        }

        ClearSelection();
    }

    private void OnToolChanged(EditorToolChangedEvent e)
    {
        if (_activeTool == e.Tool)
        {
            return;
        }

        _activeTool = e.Tool;
        _lineTool.Cancel();
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        base.OnOpenGlInit(gl);

        Log.Information("OpenGL contexto: {Version} (perfil={Profile})", gl.Version, GlVersion.Type);

        try
        {
            _gl = GL.GetApi(gl.GetProcAddress);
            _engine.Initialize(_gl);

            // A primeira sincronização acontece aqui; as seguintes rodam a cada frame em
            // OnOpenGlRender, para acompanhar o crescimento incremental das malhas (ferramenta Linha).
            _engine.SyncMeshes(_scene.Meshes);
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
            _cameraController.Update(deltaSeconds);

            // Avalonia espera que o conteúdo seja desenhado no framebuffer indicado por "fb"
            // (não necessariamente o 0/padrão), que ela compõe no restante da árvore visual.
            _gl!.BindFramebuffer(GLEnum.Framebuffer, (uint)fb);

            // Reenvia buffers de malhas que a ferramenta Linha alterou desde o último frame
            // (Mesh.Version) — barato quando nada mudou, é só uma comparação de inteiro por malha.
            _engine.SyncMeshes(_scene.Meshes);

            _engine.Resize(pixelWidth, pixelHeight);
            _engine.Render(
                _camera.GetViewMatrix(),
                _camera.GetProjectionMatrix(),
                _camera.Position,
                deltaSeconds,
                _scene.Meshes,
                BuildHighlight,
                BuildLinePreview());

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
        _toolSubscription.Dispose();
        _selection.Dispose();
        _engine.Dispose();
        base.OnOpenGlDeinit(gl);
    }

    private LinePreview? BuildLinePreview()
    {
        if (_activeTool != EditorTool.Line || _pointerPosition is not { } pointer)
        {
            return null;
        }

        var viewportSize = new Vector2((float)Bounds.Width, (float)Bounds.Height);
        var screenPoint = new Vector2((float)pointer.X, (float)pointer.Y);

        var snap = _lineTool.Preview(screenPoint, viewportSize, _camera.Position, _camera.GetViewMatrix(), _camera.GetProjectionMatrix());
        var markerColor = snap.Kind switch
        {
            LineSnapKind.Endpoint => EndpointSnapColor,
            LineSnapKind.AxisX => AxesAxisColor(0),
            LineSnapKind.AxisY => AxesAxisColor(1),
            LineSnapKind.AxisZ => AxesAxisColor(2),
            _ => PlaneSnapColor,
        };

        return new LinePreview(_lineTool.StartPoint, snap.Position, markerColor, LineDrawColor);
    }

    // Mesmas cores do AxesRenderer (X=vermelho, Y=verde, Z=azul), para o indicador de snap por eixo
    // comunicar visualmente a mesma convenção que os eixos desenhados na cena.
    private static Vector3 AxesAxisColor(int axisIndex) => axisIndex switch
    {
        0 => new Vector3(0.85f, 0.25f, 0.25f),
        1 => new Vector3(0.30f, 0.75f, 0.30f),
        _ => new Vector3(0.25f, 0.45f, 0.90f),
    };

    private MeshHighlight? BuildHighlight(Mesh mesh)
    {
        List<int>? vertices = null;
        List<int>? edges = null;
        List<int>? faces = null;
        var wholeObject = false;

        foreach (var element in _selection.Selected)
        {
            if (element.MeshId != mesh.Id)
            {
                continue;
            }

            switch (element.Kind)
            {
                case SelectionMode.Vertex:
                    (vertices ??= []).Add(element.Index);
                    break;
                case SelectionMode.Edge:
                    (edges ??= []).Add(element.Index);
                    break;
                case SelectionMode.Face:
                    (faces ??= []).Add(element.Index);
                    break;
                case SelectionMode.Object:
                    wholeObject = true;
                    break;
            }
        }

        if (!wholeObject && vertices is null && edges is null && faces is null)
        {
            return null;
        }

        return new MeshHighlight
        {
            WholeObject = wholeObject,
            Vertices = (IReadOnlyCollection<int>?)vertices ?? [],
            Edges = (IReadOnlyCollection<int>?)edges ?? [],
            Faces = (IReadOnlyCollection<int>?)faces ?? [],
        };
    }
}
