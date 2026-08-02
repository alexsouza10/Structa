using System.Numerics;
using Silk.NET.OpenGL;
using Structa.Geometry;
using Structa.Rendering.Scene;

namespace Structa.Rendering;

/// <summary>
/// Motor de renderização da viewport: limpa o frame e desenha o conteúdo da cena (grid, eixos e
/// malhas). Não conhece Avalonia, o módulo Camera nem o de Seleção — recebe apenas as matrizes e o
/// destaque já resolvidos, mantendo este módulo independente da UI.
/// </summary>
public sealed class RenderEngine : IDisposable
{
    private GL? _gl;
    private GridRenderer? _grid;
    private AxesRenderer? _axes;
    private LinePreviewRenderer? _linePreview;
    private readonly Dictionary<Guid, MeshRenderer> _meshRenderers = [];
    private readonly Dictionary<Guid, int> _uploadedVersions = [];

    public Vector3 BackgroundColor { get; set; } = new(0.106f, 0.106f, 0.122f);

    public FrameStats Stats { get; } = new();

    public void Initialize(GL gl)
    {
        _gl = gl;
        _gl.Enable(EnableCap.DepthTest);
        _gl.DepthFunc(DepthFunction.Lequal);

        _grid = new GridRenderer(gl);
        _axes = new AxesRenderer(gl);
        _linePreview = new LinePreviewRenderer(gl);
    }

    public void Resize(uint pixelWidth, uint pixelHeight)
    {
        _gl?.Viewport(0, 0, pixelWidth, pixelHeight);
    }

    /// <summary>
    /// Garante que cada malha da cena tenha buffers de GPU atualizados: faz upload das que ainda não
    /// têm renderer e reenvia as que mudaram desde o último frame (<see cref="Mesh.Version"/> — é o
    /// caso das malhas que ferramentas de desenho, como a de Linha, crescem incrementalmente).
    /// </summary>
    public void SyncMeshes(IReadOnlyList<Mesh> meshes)
    {
        if (_gl is null)
        {
            return;
        }

        foreach (var mesh in meshes)
        {
            if (_meshRenderers.TryGetValue(mesh.Id, out var renderer))
            {
                if (_uploadedVersions[mesh.Id] != mesh.Version)
                {
                    renderer.Upload(mesh);
                    _uploadedVersions[mesh.Id] = mesh.Version;
                }

                continue;
            }

            renderer = new MeshRenderer(_gl);
            renderer.Upload(mesh);
            _meshRenderers[mesh.Id] = renderer;
            _uploadedVersions[mesh.Id] = mesh.Version;
        }
    }

    public void Render(
        in Matrix4x4 view,
        in Matrix4x4 projection,
        Vector3 cameraPosition,
        double deltaSeconds,
        IReadOnlyList<Mesh>? meshes = null,
        Func<Mesh, MeshHighlight?>? highlightSelector = null,
        LinePreview? linePreview = null)
    {
        if (_gl is null || _grid is null || _axes is null)
        {
            return;
        }

        Stats.Tick(deltaSeconds);

        _gl.ClearColor(BackgroundColor.X, BackgroundColor.Y, BackgroundColor.Z, 1f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _grid.Render(view, projection, cameraPosition);
        _axes.Render(view, projection);

        if (meshes is not null)
        {
            foreach (var mesh in meshes)
            {
                if (_meshRenderers.TryGetValue(mesh.Id, out var renderer))
                {
                    renderer.Render(view, projection, highlightSelector?.Invoke(mesh));
                }
            }
        }

        if (linePreview is { } preview)
        {
            _linePreview?.Render(view, projection, preview.SegmentStart, preview.CursorPoint, preview.MarkerColor, preview.LineColor);
        }
    }

    public void Dispose()
    {
        _grid?.Dispose();
        _axes?.Dispose();
        _linePreview?.Dispose();

        foreach (var renderer in _meshRenderers.Values)
        {
            renderer.Dispose();
        }
    }
}
