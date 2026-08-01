using System.Numerics;
using Silk.NET.OpenGL;
using Structa.Rendering.Buffers;
using Structa.Rendering.Shaders;

namespace Structa.Rendering.Scene;

/// <summary>
/// Plano de grid procedural no plano XY (Z=0). O efeito de "infinito" vem do shader
/// (linhas geradas por fragmento + fade radial), não de um mesh literalmente sem limites.
/// </summary>
public sealed class GridRenderer : IDisposable
{
    private const float PlaneExtent = 2000f;

    private readonly GL _gl;
    private readonly ShaderProgram _shader;
    private readonly VertexArrayObject _vao;
    private readonly BufferObject<float> _vbo;

    public Vector3 MinorLineColor { get; set; } = new(0.40f, 0.40f, 0.44f);

    public Vector3 MajorLineColor { get; set; } = new(0.65f, 0.65f, 0.70f);

    public float FadeDistance { get; set; } = 120f;

    public GridRenderer(GL gl)
    {
        _gl = gl;
        _shader = new ShaderProgram(gl, GridShaderSource.Vertex, GridShaderSource.Fragment);

        const float e = PlaneExtent;
        ReadOnlySpan<float> vertices =
        [
            -e, -e, 0f,
             e, -e, 0f,
             e,  e, 0f,
            -e, -e, 0f,
             e,  e, 0f,
            -e,  e, 0f,
        ];

        _vao = new VertexArrayObject(gl);
        _vao.Bind();
        _vbo = new BufferObject<float>(gl, vertices, BufferTargetARB.ArrayBuffer);
        _vao.SetVertexAttribute(0, 3, 3 * sizeof(float), 0);
    }

    public void Render(in Matrix4x4 view, in Matrix4x4 projection, Vector3 cameraPosition)
    {
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _shader.Use();
        _shader.SetMatrix4("uView", view);
        _shader.SetMatrix4("uProjection", projection);
        _shader.SetVector3("uCameraPosition", cameraPosition);
        _shader.SetVector3("uMinorLineColor", MinorLineColor);
        _shader.SetVector3("uMajorLineColor", MajorLineColor);
        _shader.SetFloat("uFadeDistance", FadeDistance);

        _vao.Bind();
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

        _gl.Disable(EnableCap.Blend);
    }

    public void Dispose()
    {
        _vbo.Dispose();
        _vao.Dispose();
        _shader.Dispose();
    }
}
