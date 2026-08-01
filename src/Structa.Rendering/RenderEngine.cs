using System.Numerics;
using Silk.NET.OpenGL;
using Structa.Rendering.Scene;

namespace Structa.Rendering;

/// <summary>
/// Motor de renderização da viewport: limpa o frame e desenha o conteúdo da cena (grid, eixos e,
/// futuramente, geometria). Não conhece Avalonia nem a câmera do editor — recebe apenas as
/// matrizes prontas, mantendo este módulo independente da UI e do módulo Camera.
/// </summary>
public sealed class RenderEngine : IDisposable
{
    private GL? _gl;
    private GridRenderer? _grid;
    private AxesRenderer? _axes;

    public Vector3 BackgroundColor { get; set; } = new(0.106f, 0.106f, 0.122f);

    public FrameStats Stats { get; } = new();

    public void Initialize(GL gl)
    {
        _gl = gl;
        _gl.Enable(EnableCap.DepthTest);
        _gl.DepthFunc(DepthFunction.Lequal);

        _grid = new GridRenderer(gl);
        _axes = new AxesRenderer(gl);
    }

    public void Resize(uint pixelWidth, uint pixelHeight)
    {
        _gl?.Viewport(0, 0, pixelWidth, pixelHeight);
    }

    public void Render(in Matrix4x4 view, in Matrix4x4 projection, Vector3 cameraPosition, double deltaSeconds)
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
    }

    public void Dispose()
    {
        _grid?.Dispose();
        _axes?.Dispose();
    }
}
