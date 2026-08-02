using System.Numerics;
using Silk.NET.OpenGL;
using Structa.Rendering.Buffers;
using Structa.Rendering.Shaders;

namespace Structa.Rendering.Scene;

/// <summary>
/// Desenha o feedback visual da ferramenta Linha: o marcador no ponto sob o cursor (cor conforme o
/// tipo de snap) e, enquanto um segmento está pendente, a linha de arrasto do ponto inicial até ele.
/// Os buffers são pequenos e atualizados a cada frame (<see cref="BufferObject{T}.SetData"/>) — o custo
/// é irrelevante comparado ao upload de uma malha completa.
/// </summary>
public sealed class LinePreviewRenderer : IDisposable
{
    private const float LineWidth = 2f;
    private const float MarkerPointSize = 10f;

    private readonly GL _gl;
    private readonly ShaderProgram _shader;
    private readonly VertexArrayObject _lineVao;
    private readonly BufferObject<float> _lineVbo;
    private readonly VertexArrayObject _markerVao;
    private readonly BufferObject<float> _markerVbo;

    public LinePreviewRenderer(GL gl)
    {
        _gl = gl;
        _shader = new ShaderProgram(gl, FlatColorShaderSource.Vertex, FlatColorShaderSource.Fragment);

        _lineVao = new VertexArrayObject(gl);
        _lineVao.Bind();
        _lineVbo = new BufferObject<float>(gl, stackalloc float[6], BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);
        _lineVao.SetVertexAttribute(0, 3, 3 * sizeof(float), 0);

        _markerVao = new VertexArrayObject(gl);
        _markerVao.Bind();
        _markerVbo = new BufferObject<float>(gl, stackalloc float[3], BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);
        _markerVao.SetVertexAttribute(0, 3, 3 * sizeof(float), 0);
    }

    /// <summary>
    /// <paramref name="segmentStart"/> nulo desenha só o marcador (hover, antes do primeiro clique);
    /// com valor, desenha também a linha de arrasto até <paramref name="cursorPoint"/>.
    /// </summary>
    public void Render(
        in Matrix4x4 view,
        in Matrix4x4 projection,
        Vector3? segmentStart,
        Vector3 cursorPoint,
        Vector3 markerColor,
        Vector3 lineColor)
    {
        // Sempre visível por cima da geometria, como a linha de inferência do SketchUp.
        _gl.Disable(EnableCap.DepthTest);

        _shader.Use();
        _shader.SetMatrix4("uView", view);
        _shader.SetMatrix4("uProjection", projection);

        if (segmentStart is { } start)
        {
            Span<float> lineData = [start.X, start.Y, start.Z, cursorPoint.X, cursorPoint.Y, cursorPoint.Z];
            _lineVbo.SetData(lineData);

            _gl.LineWidth(LineWidth);
            _shader.SetVector3("uColor", lineColor);
            _lineVao.Bind();
            _gl.DrawArrays(PrimitiveType.Lines, 0, 2);
        }

        Span<float> markerData = [cursorPoint.X, cursorPoint.Y, cursorPoint.Z];
        _markerVbo.SetData(markerData);

        _shader.SetVector3("uColor", markerColor);
        _shader.SetFloat("uPointSize", MarkerPointSize);
        _markerVao.Bind();
        _gl.DrawArrays(PrimitiveType.Points, 0, 1);

        _gl.Enable(EnableCap.DepthTest);
    }

    public void Dispose()
    {
        _lineVbo.Dispose();
        _lineVao.Dispose();
        _markerVbo.Dispose();
        _markerVao.Dispose();
        _shader.Dispose();
    }
}
