using System.Numerics;

namespace Structa.Geometry;

/// <summary>
/// Topologia lógica de uma malha: vértices e as arestas/triângulos que os conectam, referenciados
/// por índice. É a forma "selecionável" da geometria — a malha usada para desenho (com normais
/// duplicadas por face etc.) é derivada disto pelo <c>MeshRenderer</c>, não faz parte deste tipo.
///
/// Mutável por construção: a partir da Etapa 05, ferramentas de desenho (ex.: <c>LineTool</c>)
/// crescem a malha incrementalmente. <see cref="Version"/> é incrementado a cada mutação para que o
/// <c>RenderEngine</c> saiba quando reenviar os buffers de GPU, sem precisar comparar conteúdo.
/// </summary>
public sealed class Mesh
{
    private readonly List<Vector3> _vertices;
    private readonly List<(int A, int B)> _edges;
    private readonly List<(int A, int B, int C)> _triangles;

    public Mesh(
        string name,
        IEnumerable<Vector3>? vertices = null,
        IEnumerable<(int A, int B)>? edges = null,
        IEnumerable<(int A, int B, int C)>? triangles = null)
    {
        Name = name;
        _vertices = vertices is null ? [] : [.. vertices];
        _edges = edges is null ? [] : [.. edges];
        _triangles = triangles is null ? [] : [.. triangles];
    }

    public Guid Id { get; } = Guid.NewGuid();

    public string Name { get; }

    public IReadOnlyList<Vector3> Vertices => _vertices;

    public IReadOnlyList<(int A, int B)> Edges => _edges;

    public IReadOnlyList<(int A, int B, int C)> Triangles => _triangles;

    public int Version { get; private set; }

    /// <summary>Adiciona um vértice e retorna seu índice.</summary>
    public int AddVertex(Vector3 position)
    {
        _vertices.Add(position);
        Version++;
        return _vertices.Count - 1;
    }

    public bool HasEdge(int a, int b) => _edges.Contains((a, b)) || _edges.Contains((b, a));

    /// <summary>Adiciona uma aresta entre dois vértices já existentes na malha (por índice).</summary>
    public void AddEdge(int a, int b)
    {
        _edges.Add((a, b));
        Version++;
    }

    /// <summary>Verdadeiro se algum triângulo já usa exatamente esses três vértices (em qualquer ordem).</summary>
    public bool HasTriangle(int a, int b, int c)
    {
        foreach (var (ta, tb, tc) in _triangles)
        {
            if (IsSameVertexSet(a, b, c, ta, tb, tc))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Adiciona uma face triangular entre três vértices já existentes na malha (por índice).
    /// A ordem define a face frontal (normal = regra da mão direita sobre a-&gt;b-&gt;c).</summary>
    public void AddTriangle(int a, int b, int c)
    {
        _triangles.Add((a, b, c));
        Version++;
    }

    private static bool IsSameVertexSet(int a1, int b1, int c1, int a2, int b2, int c2) =>
        (a1 == a2 || a1 == b2 || a1 == c2) &&
        (b1 == a2 || b1 == b2 || b1 == c2) &&
        (c1 == a2 || c1 == b2 || c1 == c2);
}
