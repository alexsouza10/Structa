using System.Numerics;
using Silk.NET.OpenGL;
using Structa.Rendering.Buffers;
using Structa.Rendering.Shaders;

namespace Structa.Rendering.Scene;

/// <summary>
/// Desenha uma lista arbitrária de segmentos de reta independentes (cada par consecutivo de pontos é
/// uma linha, como <see cref="PrimitiveType.Lines"/> espera) — o "fantasma" usado pelo preview do
/// Push/Pull para mostrar onde a face vai parar antes de confirmar a extrusão. Reenvia o buffer a cada
/// chamada (<see cref="BufferObject{T}.SetData"/>): o número de segmentos muda a cada frame conforme o
/// contorno da face sendo puxada, e o volume de dados é pequeno o bastante para isso ser irrelevante.
/// </summary>
public sealed class GhostOutlineRenderer : IDisposable
{
    private const float LineWidth = 2f;

    private readonly GL _gl;
    private readonly ShaderProgram _shader;
    private readonly VertexArrayObject _vao;
    private readonly BufferObject<float> _vbo;

    public GhostOutlineRenderer(GL gl)
    {
        _gl = gl;
        _shader = new ShaderProgram(gl, FlatColorShaderSource.Vertex, FlatColorShaderSource.Fragment);

        _vao = new VertexArrayObject(gl);
        _vao.Bind();
        _vbo = new BufferObject<float>(gl, stackalloc float[6], BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);
        _vao.SetVertexAttribute(0, 3, 3 * sizeof(float), 0);
    }

    /// <summary><paramref name="segmentPoints"/>: pontos em pares consecutivos (0-1, 2-3, ...), cada par
    /// uma linha independente. Ímpar ou vazio: não desenha nada.</summary>
    public void Render(in Matrix4x4 view, in Matrix4x4 projection, IReadOnlyList<Vector3> segmentPoints, Vector3 color)
    {
        if (segmentPoints.Count < 2)
        {
            return;
        }

        var data = new float[segmentPoints.Count * 3];
        for (var i = 0; i < segmentPoints.Count; i++)
        {
            data[(i * 3) + 0] = segmentPoints[i].X;
            data[(i * 3) + 1] = segmentPoints[i].Y;
            data[(i * 3) + 2] = segmentPoints[i].Z;
        }

        _vbo.SetData(data);

        // Sempre visível por cima da geometria, como o preview da ferramenta Linha.
        _gl.Disable(EnableCap.DepthTest);
        _gl.LineWidth(LineWidth);

        _shader.Use();
        _shader.SetMatrix4("uView", view);
        _shader.SetMatrix4("uProjection", projection);
        _shader.SetVector3("uColor", color);

        _vao.Bind();
        _gl.DrawArrays(PrimitiveType.Lines, 0, (uint)(segmentPoints.Count - (segmentPoints.Count % 2)));

        _gl.Enable(EnableCap.DepthTest);
    }

    public void Dispose()
    {
        _vbo.Dispose();
        _vao.Dispose();
        _shader.Dispose();
    }
}
