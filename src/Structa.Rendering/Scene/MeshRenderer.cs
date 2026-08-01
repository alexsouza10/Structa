using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using Structa.Geometry;
using Structa.Rendering.Buffers;
using Structa.Rendering.Shaders;

namespace Structa.Rendering.Scene;

/// <summary>
/// Desenha uma <see cref="Mesh"/>: faces sombreadas (shader com luz fixa), wireframe e vértices
/// (shader plano), e o realce de qualquer subconjunto selecionado por cima. A malha "selecionável"
/// (vértices/arestas/triângulos por índice lógico) é convertida aqui para os buffers que a GPU
/// precisa — a triangulação com normal duplicada por face vive só neste renderer.
/// </summary>
public sealed class MeshRenderer : IDisposable
{
    private static readonly Vector3 BaseFaceColor = new(0.55f, 0.56f, 0.60f);
    private static readonly Vector3 BaseEdgeColor = new(0.06f, 0.06f, 0.07f);
    private static readonly Vector3 BaseVertexColor = new(0.15f, 0.15f, 0.17f);
    private static readonly Vector3 HighlightColor = new(1.0f, 0.55f, 0.1f);

    private const float BaseVertexPointSize = 4f;
    private const float HighlightVertexPointSize = 9f;

    private readonly GL _gl;
    private readonly ShaderProgram _litShader;
    private readonly ShaderProgram _flatShader;

    private VertexArrayObject? _triangleVao;
    private BufferObject<float>? _triangleVbo;
    private int _triangleCount;

    private VertexArrayObject? _edgeVao;
    private BufferObject<float>? _edgeVbo;
    private int _edgeCount;

    private VertexArrayObject? _pointVao;
    private BufferObject<float>? _pointVbo;
    private int _vertexCount;

    public MeshRenderer(GL gl)
    {
        _gl = gl;
        _litShader = new ShaderProgram(gl, MeshShaderSource.Vertex, MeshShaderSource.Fragment);
        _flatShader = new ShaderProgram(gl, FlatColorShaderSource.Vertex, FlatColorShaderSource.Fragment);
    }

    /// <summary>Reconstrói os buffers de GPU a partir da topologia lógica da malha.</summary>
    public void Upload(Mesh mesh)
    {
        DisposeBuffers();

        var triangleData = new List<float>(mesh.Triangles.Count * 3 * 6);
        foreach (var (a, b, c) in mesh.Triangles)
        {
            var pa = mesh.Vertices[a];
            var pb = mesh.Vertices[b];
            var pc = mesh.Vertices[c];
            var normal = Vector3.Normalize(Vector3.Cross(pb - pa, pc - pa));

            AppendVertex(triangleData, pa, normal);
            AppendVertex(triangleData, pb, normal);
            AppendVertex(triangleData, pc, normal);
        }

        _triangleCount = mesh.Triangles.Count;
        _triangleVao = new VertexArrayObject(_gl);
        _triangleVao.Bind();
        _triangleVbo = new BufferObject<float>(_gl, CollectionsMarshal.AsSpan(triangleData), BufferTargetARB.ArrayBuffer);
        _triangleVao.SetVertexAttribute(0, 3, 6 * sizeof(float), 0);
        _triangleVao.SetVertexAttribute(1, 3, 6 * sizeof(float), 3 * sizeof(float));

        var edgeData = new List<float>(mesh.Edges.Count * 2 * 3);
        foreach (var (a, b) in mesh.Edges)
        {
            AppendPosition(edgeData, mesh.Vertices[a]);
            AppendPosition(edgeData, mesh.Vertices[b]);
        }

        _edgeCount = mesh.Edges.Count;
        _edgeVao = new VertexArrayObject(_gl);
        _edgeVao.Bind();
        _edgeVbo = new BufferObject<float>(_gl, CollectionsMarshal.AsSpan(edgeData), BufferTargetARB.ArrayBuffer);
        _edgeVao.SetVertexAttribute(0, 3, 3 * sizeof(float), 0);

        var pointData = new List<float>(mesh.Vertices.Count * 3);
        foreach (var vertex in mesh.Vertices)
        {
            AppendPosition(pointData, vertex);
        }

        _vertexCount = mesh.Vertices.Count;
        _pointVao = new VertexArrayObject(_gl);
        _pointVao.Bind();
        _pointVbo = new BufferObject<float>(_gl, CollectionsMarshal.AsSpan(pointData), BufferTargetARB.ArrayBuffer);
        _pointVao.SetVertexAttribute(0, 3, 3 * sizeof(float), 0);
    }

    public void Render(in Matrix4x4 view, in Matrix4x4 projection, MeshHighlight? highlight = null)
    {
        if (_triangleVao is null || _edgeVao is null || _pointVao is null)
        {
            return;
        }

        _litShader.Use();
        _litShader.SetMatrix4("uView", view);
        _litShader.SetMatrix4("uProjection", projection);
        _litShader.SetVector3("uColor", BaseFaceColor);
        _triangleVao.Bind();
        _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)(_triangleCount * 3));

        _flatShader.Use();
        _flatShader.SetMatrix4("uView", view);
        _flatShader.SetMatrix4("uProjection", projection);

        _flatShader.SetVector3("uColor", BaseEdgeColor);
        _edgeVao.Bind();
        _gl.DrawArrays(PrimitiveType.Lines, 0, (uint)(_edgeCount * 2));

        _flatShader.SetVector3("uColor", BaseVertexColor);
        _flatShader.SetFloat("uPointSize", BaseVertexPointSize);
        _pointVao.Bind();
        _gl.DrawArrays(PrimitiveType.Points, 0, (uint)_vertexCount);

        if (highlight is null)
        {
            return;
        }

        _flatShader.SetVector3("uColor", HighlightColor);

        if (highlight.WholeObject)
        {
            _edgeVao.Bind();
            _gl.DrawArrays(PrimitiveType.Lines, 0, (uint)(_edgeCount * 2));
        }

        if (highlight.Faces.Count > 0)
        {
            _triangleVao.Bind();
            foreach (var faceIndex in highlight.Faces)
            {
                _gl.DrawArrays(PrimitiveType.Triangles, faceIndex * 3, 3);
            }
        }

        if (highlight.Edges.Count > 0)
        {
            _edgeVao.Bind();
            foreach (var edgeIndex in highlight.Edges)
            {
                _gl.DrawArrays(PrimitiveType.Lines, edgeIndex * 2, 2);
            }
        }

        if (highlight.Vertices.Count > 0)
        {
            _flatShader.SetFloat("uPointSize", HighlightVertexPointSize);
            _pointVao.Bind();
            foreach (var vertexIndex in highlight.Vertices)
            {
                _gl.DrawArrays(PrimitiveType.Points, vertexIndex, 1);
            }
        }
    }

    public void Dispose()
    {
        DisposeBuffers();
        _litShader.Dispose();
        _flatShader.Dispose();
    }

    private void DisposeBuffers()
    {
        _triangleVbo?.Dispose();
        _triangleVao?.Dispose();
        _edgeVbo?.Dispose();
        _edgeVao?.Dispose();
        _pointVbo?.Dispose();
        _pointVao?.Dispose();
    }

    private static void AppendVertex(List<float> buffer, Vector3 position, Vector3 normal)
    {
        buffer.Add(position.X);
        buffer.Add(position.Y);
        buffer.Add(position.Z);
        buffer.Add(normal.X);
        buffer.Add(normal.Y);
        buffer.Add(normal.Z);
    }

    private static void AppendPosition(List<float> buffer, Vector3 position)
    {
        buffer.Add(position.X);
        buffer.Add(position.Y);
        buffer.Add(position.Z);
    }
}
