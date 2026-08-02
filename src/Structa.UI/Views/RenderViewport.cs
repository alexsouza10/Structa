using System.Diagnostics;
using System.Globalization;
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

    public static readonly StyledProperty<string?> ToolStatusTextProperty =
        AvaloniaProperty.Register<RenderViewport, string?>(nameof(ToolStatusText));

    /// <summary>Indicador da ferramenta de arrasto ativa (distância do Empurrar/Puxar ou Mover, ângulo do
    /// Rotacionar, fator do Escalar — dígitos ou valor arrastado, formatado); nulo se nenhuma estiver ativa.</summary>
    public string? ToolStatusText
    {
        get => GetValue(ToolStatusTextProperty);
        private set => SetValue(ToolStatusTextProperty, value);
    }

    // Cores de snap do preview da ferramenta Linha: eixos usam as mesmas cores do AxesRenderer (X/Y/Z),
    // ponta existente em verde-destaque e plano livre em âmbar — cada uma comunica visualmente por que
    // o ponto está grudado ali.
    private static readonly Vector3 LineDrawColor = new(1f, 1f, 1f);
    private static readonly Vector3 EndpointSnapColor = new(0.15f, 0.90f, 0.40f);
    private static readonly Vector3 PlaneSnapColor = new(0.95f, 0.75f, 0.15f);

    // Uma cor de fantasma por ferramenta de arrasto, para diferenciar visualmente qual operação está
    // em andamento mesmo sem olhar para a barra superior.
    private static readonly Vector3 PushPullGhostColor = new(0.95f, 0.55f, 0.15f);
    private static readonly Vector3 MoveGhostColor = new(0.20f, 0.75f, 0.95f);
    private static readonly Vector3 RotateGhostColor = new(0.75f, 0.35f, 0.95f);
    private static readonly Vector3 ScaleGhostColor = new(0.65f, 0.85f, 0.25f);

    private readonly GlCamera _camera = new();
    private readonly OrbitCameraController _cameraController;
    private readonly RenderEngine _engine = new();
    private readonly Scene _scene = new();
    private readonly SelectionManager _selection;
    private readonly LineTool _lineTool;
    private readonly PushPullTool _pushPullTool;
    private readonly MoveTool _moveTool;
    private readonly RotateTool _rotateTool;
    private readonly ScaleTool _scaleTool;
    private readonly MirrorTool _mirrorTool;
    private readonly IEventAggregator _eventAggregator;
    private readonly IDisposable _toolSubscription;
    private readonly IDisposable _mirrorSubscription;
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
        _pushPullTool = new PushPullTool(_scene);
        _moveTool = new MoveTool(_scene);
        _rotateTool = new RotateTool(_scene);
        _scaleTool = new ScaleTool(_scene);
        _mirrorTool = new MirrorTool(_scene);
        _toolSubscription = _eventAggregator.Subscribe<EditorToolChangedEvent>(OnToolChanged);
        _mirrorSubscription = _eventAggregator.Subscribe<MirrorRequestedEvent>(OnMirrorRequested);

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

    /// <summary>Botão esquerdo pressionado: delega para a ferramenta ativa (seleção, linha, início do
    /// arrasto de Empurrar/Puxar, ou agarrar a seleção atual para Mover/Rotacionar/Escalar — a
    /// confirmação vem em <see cref="CommitActiveTool"/>, ao soltar). <paramref name="duplicate"/> (Ctrl)
    /// só é usado pela ferramenta Mover.</summary>
    public void HandlePrimaryClick(Point screenPoint, bool additive, bool duplicate)
    {
        var viewportSize = new Vector2((float)Bounds.Width, (float)Bounds.Height);
        var pixelPoint = new Vector2((float)screenPoint.X, (float)screenPoint.Y);
        var view = _camera.GetViewMatrix();
        var projection = _camera.GetProjectionMatrix();

        switch (_activeTool)
        {
            case EditorTool.Line:
                _lineTool.Click(pixelPoint, viewportSize, _camera.Position, view, projection);
                return;
            case EditorTool.PushPull:
                _pushPullTool.TryBegin(pixelPoint, viewportSize, _camera.Position, view, projection);
                return;
            case EditorTool.Move:
                _moveTool.TryBegin(_selection.Selected, duplicate, pixelPoint, viewportSize, _camera.Position, view, projection);
                return;
            case EditorTool.Rotate:
                _rotateTool.TryBegin(_selection.Selected, pixelPoint, viewportSize, _camera.Position, view, projection);
                return;
            case EditorTool.Scale:
                _scaleTool.TryBegin(_selection.Selected, pixelPoint, viewportSize, _camera.Position, view, projection);
                return;
            default:
                Pick(screenPoint, additive);
                return;
        }
    }

    /// <summary>Verdadeiro enquanto qualquer ferramenta de arrasto (Empurrar/Puxar, Mover, Rotacionar,
    /// Escalar) está em andamento — usado pela view para saber se deve rotear dígitos/Enter para ela.</summary>
    public bool IsTransformToolActive => _pushPullTool.IsActive || _moveTool.IsActive || _rotateTool.IsActive || _scaleTool.IsActive;

    /// <summary>Confirma a operação da ferramenta de arrasto ativa com o valor efetivo atual (arrastado ou
    /// digitado). Chamado ao soltar o botão esquerdo ou pressionar Enter; sem efeito se nenhuma estiver ativa.</summary>
    public void CommitActiveTool()
    {
        if (_pushPullTool.IsActive)
        {
            _pushPullTool.Commit();
        }
        else if (_moveTool.IsActive)
        {
            _moveTool.Commit();
        }
        else if (_rotateTool.IsActive)
        {
            _rotateTool.Commit();
        }
        else if (_scaleTool.IsActive)
        {
            _scaleTool.Commit();
        }
    }

    public void AppendActiveToolCharacter(char character)
    {
        if (_pushPullTool.IsActive)
        {
            _pushPullTool.AppendDistanceCharacter(character);
        }
        else if (_moveTool.IsActive)
        {
            _moveTool.AppendDistanceCharacter(character);
        }
        else if (_rotateTool.IsActive)
        {
            _rotateTool.AppendAngleCharacter(character);
        }
        else if (_scaleTool.IsActive)
        {
            _scaleTool.AppendFactorCharacter(character);
        }
    }

    public void RemoveActiveToolCharacter()
    {
        if (_pushPullTool.IsActive)
        {
            _pushPullTool.RemoveLastDistanceCharacter();
        }
        else if (_moveTool.IsActive)
        {
            _moveTool.RemoveLastDistanceCharacter();
        }
        else if (_rotateTool.IsActive)
        {
            _rotateTool.RemoveLastAngleCharacter();
        }
        else if (_scaleTool.IsActive)
        {
            _scaleTool.RemoveLastFactorCharacter();
        }
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

    /// <summary>Esc: cancela a operação pendente da ferramenta ativa (arrasto de Empurrar/Puxar, Mover,
    /// Rotacionar, Escalar, ou segmento de Linha); sem operação pendente, limpa a seleção.</summary>
    public void CancelActiveTool()
    {
        if (_pushPullTool.IsActive)
        {
            _pushPullTool.Cancel();
            return;
        }

        if (_moveTool.IsActive)
        {
            _moveTool.Cancel();
            return;
        }

        if (_rotateTool.IsActive)
        {
            _rotateTool.Cancel();
            return;
        }

        if (_scaleTool.IsActive)
        {
            _scaleTool.Cancel();
            return;
        }

        if (_lineTool.IsDrawing)
        {
            _lineTool.Cancel();
            return;
        }

        ClearSelection();
    }

    private void OnMirrorRequested(MirrorRequestedEvent e) => _mirrorTool.Mirror(_selection.Selected, e.Axis);

    private void OnToolChanged(EditorToolChangedEvent e)
    {
        if (_activeTool == e.Tool)
        {
            return;
        }

        _activeTool = e.Tool;
        _lineTool.Cancel();
        _pushPullTool.Cancel();
        _moveTool.Cancel();
        _rotateTool.Cancel();
        _scaleTool.Cancel();
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

            if (_pointerPosition is { } dragPointer)
            {
                var viewportSize = new Vector2((float)Bounds.Width, (float)Bounds.Height);
                var pixelPoint = new Vector2((float)dragPointer.X, (float)dragPointer.Y);
                var view = _camera.GetViewMatrix();
                var projection = _camera.GetProjectionMatrix();

                if (_pushPullTool.IsActive)
                {
                    _pushPullTool.UpdateDrag(pixelPoint, viewportSize, _camera.Position, view, projection);
                }
                else if (_moveTool.IsActive)
                {
                    _moveTool.UpdateDrag(pixelPoint, viewportSize, _camera.Position, view, projection);
                }
                else if (_rotateTool.IsActive)
                {
                    _rotateTool.UpdateDrag(pixelPoint, viewportSize, _camera.Position, view, projection);
                }
                else if (_scaleTool.IsActive)
                {
                    _scaleTool.UpdateDrag(pixelPoint, viewportSize, _camera.Position, view, projection);
                }
            }

            // Reenvia buffers de malhas que as ferramentas de desenho/transformação alteraram desde o
            // último frame (Mesh.Version) — barato quando nada mudou, é só uma comparação de inteiro por malha.
            _engine.SyncMeshes(_scene.Meshes);

            _engine.Resize(pixelWidth, pixelHeight);
            _engine.Render(
                _camera.GetViewMatrix(),
                _camera.GetProjectionMatrix(),
                _camera.Position,
                deltaSeconds,
                _scene.Meshes,
                BuildHighlight,
                BuildLinePreview(),
                BuildGhostPreview());

            var fps = Math.Round(_engine.Stats.FramesPerSecond);
            var statusText = BuildStatusText();
            Dispatcher.UIThread.Post(() =>
            {
                FramesPerSecond = fps;
                ToolStatusText = statusText;
            });
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
        _mirrorSubscription.Dispose();
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

    /// <summary>Monta o "fantasma" da ferramenta de arrasto ativa (só uma por vez): Empurrar/Puxar desenha
    /// o contorno deslocado + conectores verticais; Mover/Rotacionar/Escalar desenham o wireframe da
    /// seleção já transformado (<see cref="GhostGeometry"/>). Formato esperado pelo <see cref="GhostOutlineRenderer"/>:
    /// pontos em pares consecutivos, cada par uma linha independente.</summary>
    private GhostPreview? BuildGhostPreview()
    {
        if (_pushPullTool.IsActive)
        {
            return BuildPushPullGhost();
        }

        if (_moveTool.IsActive && _moveTool.GetPreviewSegments() is { Count: > 0 } moveSegments)
        {
            return new GhostPreview(moveSegments, MoveGhostColor);
        }

        if (_rotateTool.IsActive && _rotateTool.GetPreviewSegments() is { Count: > 0 } rotateSegments)
        {
            return new GhostPreview(rotateSegments, RotateGhostColor);
        }

        if (_scaleTool.IsActive && _scaleTool.GetPreviewSegments() is { Count: > 0 } scaleSegments)
        {
            return new GhostPreview(scaleSegments, ScaleGhostColor);
        }

        return null;
    }

    private GhostPreview? BuildPushPullGhost()
    {
        if (_pushPullTool.GetBoundaryLoopPositions() is not { Count: > 0 } loop)
        {
            return null;
        }

        var offset = _pushPullTool.CurrentOffset;
        var segments = new List<Vector3>(loop.Count * 4);

        for (var i = 0; i < loop.Count; i++)
        {
            var basePoint = loop[i];
            var topPoint = basePoint + offset;
            var nextTopPoint = loop[(i + 1) % loop.Count] + offset;

            segments.Add(basePoint);
            segments.Add(topPoint);

            segments.Add(topPoint);
            segments.Add(nextTopPoint);
        }

        return new GhostPreview(segments, PushPullGhostColor);
    }

    /// <summary>Rótulo do indicador na viewport, já com prefixo (unidade varia por ferramenta) — nulo se
    /// nenhuma ferramenta de arrasto estiver ativa.</summary>
    private string? BuildStatusText()
    {
        if (_pushPullTool.IsActive)
        {
            var value = _pushPullTool.TypedDistanceText ?? _pushPullTool.CurrentDistance.ToString("0.00", CultureInfo.InvariantCulture);
            return $"Distância: {value}";
        }

        if (_moveTool.IsActive)
        {
            var value = _moveTool.TypedDistanceText ?? _moveTool.CurrentDelta.Length().ToString("0.00", CultureInfo.InvariantCulture);
            var axisSuffix = _moveTool.LockedAxisIndex switch
            {
                0 => " · Eixo X",
                1 => " · Eixo Y",
                2 => " · Eixo Z",
                _ => string.Empty,
            };
            return $"Distância: {value}{axisSuffix}";
        }

        if (_rotateTool.IsActive)
        {
            var value = _rotateTool.TypedAngleText ?? (_rotateTool.CurrentAngleRadians * 180f / MathF.PI).ToString("0.0", CultureInfo.InvariantCulture);
            return $"Ângulo: {value}°";
        }

        if (_scaleTool.IsActive)
        {
            var value = _scaleTool.TypedFactorText ?? _scaleTool.CurrentFactor.ToString("0.00", CultureInfo.InvariantCulture);
            return $"Fator: {value}";
        }

        return null;
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
