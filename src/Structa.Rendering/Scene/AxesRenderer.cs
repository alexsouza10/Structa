using System.Numerics;
using Silk.NET.OpenGL;
using Structa.Rendering.Buffers;
using Structa.Rendering.Shaders;

namespace Structa.Rendering.Scene;

/// <summary>
/// Eixos XYZ (convenção SketchUp: X=vermelho, Y=verde, Z=azul, Z para cima).
/// </summary>
public sealed class AxesRenderer : IDisposable
{
    private const float AxisLength = 2000f;

    // Pequeno deslocamento em Z para evitar z-fighting com o plano do grid (também em Z=0).
    private const float ZOffset = 0.01f;

    private readonly GL _gl;
    private readonly ShaderProgram _shader;
    private readonly VertexArrayObject _vao;
    private readonly BufferObject<float> _vbo;

    public AxesRenderer(GL gl)
    {
        _gl = gl;
        _shader = new ShaderProgram(gl, AxesShaderSource.Vertex, AxesShaderSource.Fragment);

        const float l = AxisLength;
        const float z = ZOffset;

        // posição (xyz) + cor (rgb) por vértice
        ReadOnlySpan<float> vertices =
        [
            -l, 0f, z, 0.85f, 0.25f, 0.25f,
             l, 0f, z, 0.85f, 0.25f, 0.25f,

            0f, -l, z, 0.30f, 0.75f, 0.30f,
            0f,  l, z, 0.30f, 0.75f, 0.30f,

            0f, 0f, -l, 0.25f, 0.45f, 0.90f,
            0f, 0f,  l, 0.25f, 0.45f, 0.90f,
        ];

        _vao = new VertexArrayObject(gl);
        _vao.Bind();
        _vbo = new BufferObject<float>(gl, vertices, BufferTargetARB.ArrayBuffer);

        const uint stride = 6 * sizeof(float);
        _vao.SetVertexAttribute(0, 3, stride, 0);
        _vao.SetVertexAttribute(1, 3, stride, 3 * sizeof(float));
    }

    public void Render(in Matrix4x4 view, in Matrix4x4 projection)
    {
        _gl.LineWidth(1.5f);

        _shader.Use();
        _shader.SetMatrix4("uView", view);
        _shader.SetMatrix4("uProjection", projection);

        _vao.Bind();
        _gl.DrawArrays(PrimitiveType.Lines, 0, 6);
    }

    public void Dispose()
    {
        _vbo.Dispose();
        _vao.Dispose();
        _shader.Dispose();
    }
}
