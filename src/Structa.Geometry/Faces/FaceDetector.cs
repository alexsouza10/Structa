using System.Numerics;

namespace Structa.Geometry.Faces;

/// <summary>
/// Detecta e cria faces automaticamente quando um novo segmento fecha um polígono, como no SketchUp:
/// ao ligar dois vértices que já tinham um caminho entre si pelas arestas existentes, esse caminho +
/// o novo segmento formam um loop fechado, que vira face se for (aproximadamente) plano.
///
/// Não depende de nenhuma ferramenta específica — quem adiciona uma aresta a uma <see cref="Mesh"/>
/// (hoje, o <c>LineTool</c>; no futuro, Arco/Retângulo/Círculo) chama <see cref="TryDetectFace"/> logo
/// em seguida, passando os dois vértices da aresta recém-criada.
/// </summary>
public static class FaceDetector
{
    // Tolerância de planaridade: proporcional ao "raio" do loop (funciona em qualquer escala de
    // desenho) com um piso absoluto para loops minúsculos onde o ruído numérico dominaria a proporção.
    private const float PlanarToleranceRatio = 0.01f;
    private const float MinPlanarTolerance = 1e-4f;

    /// <summary>
    /// Tenta detectar o loop fechado pelo segmento <paramref name="edgeStart"/>-<paramref name="edgeEnd"/>
    /// (o menor caminho existente entre os dois, mais essa aresta) e, se ele for plano, triangula e
    /// adiciona a face à malha. Retorna false sem alterar a malha se não houver loop, se ele não for
    /// plano o suficiente, ou se a face já existir.
    /// </summary>
    public static bool TryDetectFace(Mesh mesh, int edgeStart, int edgeEnd)
    {
        var loop = FindShortestCycle(mesh, edgeStart, edgeEnd);
        if (loop is null || loop.Count < 3)
        {
            return false;
        }

        if (loop.Count == 3 && mesh.HasTriangle(loop[0], loop[1], loop[2]))
        {
            return false;
        }

        var points = new Vector3[loop.Count];
        for (var i = 0; i < loop.Count; i++)
        {
            points[i] = mesh.Vertices[loop[i]];
        }

        if (!TryFitPlane(points, out var centroid, out var normal) || !IsPlanar(points, centroid, normal))
        {
            return false;
        }

        var (u, v) = BuildPlaneBasis(normal);
        var projected = new Vector2[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            var offset = points[i] - centroid;
            projected[i] = new Vector2(Vector3.Dot(offset, u), Vector3.Dot(offset, v));
        }

        var localTriangles = PolygonTriangulator.Triangulate(projected);
        if (localTriangles.Count == 0)
        {
            return false;
        }

        foreach (var (a, b, c) in localTriangles)
        {
            mesh.AddTriangle(loop[a], loop[b], loop[c]);
        }

        return true;
    }

    /// <summary>
    /// Busca em largura o caminho mais curto de <paramref name="edgeEnd"/> até <paramref name="edgeStart"/>
    /// usando as arestas existentes (excluindo a aresta edgeStart-edgeEnd em si) — o menor caminho dá o
    /// menor loop fechado por essa aresta, equivalente a "criar a menor face possível" do SketchUp.
    /// Retorna os vértices do loop em ordem ao redor do polígono (começando em edgeStart), ou null se
    /// os dois vértices não estiverem conectados por nenhum outro caminho.
    /// </summary>
    private static List<int>? FindShortestCycle(Mesh mesh, int edgeStart, int edgeEnd)
    {
        var adjacency = BuildAdjacency(mesh, edgeStart, edgeEnd);

        var previous = new Dictionary<int, int>();
        var visited = new HashSet<int> { edgeEnd };
        var queue = new Queue<int>();
        queue.Enqueue(edgeEnd);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (!adjacency.TryGetValue(current, out var neighbors))
            {
                continue;
            }

            foreach (var next in neighbors)
            {
                if (!visited.Add(next))
                {
                    continue;
                }

                previous[next] = current;

                if (next == edgeStart)
                {
                    return ReconstructPath(previous, edgeStart, edgeEnd);
                }

                queue.Enqueue(next);
            }
        }

        return null;
    }

    private static Dictionary<int, List<int>> BuildAdjacency(Mesh mesh, int excludeA, int excludeB)
    {
        var adjacency = new Dictionary<int, List<int>>();

        foreach (var (x, y) in mesh.Edges)
        {
            if ((x == excludeA && y == excludeB) || (x == excludeB && y == excludeA))
            {
                continue;
            }

            AddDirected(adjacency, x, y);
            AddDirected(adjacency, y, x);
        }

        return adjacency;
    }

    private static void AddDirected(Dictionary<int, List<int>> adjacency, int from, int to)
    {
        if (!adjacency.TryGetValue(from, out var neighbors))
        {
            neighbors = [];
            adjacency[from] = neighbors;
        }

        neighbors.Add(to);
    }

    private static List<int> ReconstructPath(Dictionary<int, int> previous, int from, int to)
    {
        var path = new List<int> { from };
        var node = from;

        while (node != to)
        {
            node = previous[node];
            path.Add(node);
        }

        return path;
    }

    /// <summary>Normal e centroide do polígono via método de Newell — funciona mesmo para loops
    /// côncavos e é razoavelmente robusto a pequenos desvios de planaridade.</summary>
    private static bool TryFitPlane(IReadOnlyList<Vector3> points, out Vector3 centroid, out Vector3 normal)
    {
        var accumulatedNormal = Vector3.Zero;
        var sum = Vector3.Zero;

        for (var i = 0; i < points.Count; i++)
        {
            var current = points[i];
            var next = points[(i + 1) % points.Count];

            accumulatedNormal.X += (current.Y - next.Y) * (current.Z + next.Z);
            accumulatedNormal.Y += (current.Z - next.Z) * (current.X + next.X);
            accumulatedNormal.Z += (current.X - next.X) * (current.Y + next.Y);

            sum += current;
        }

        centroid = sum / points.Count;

        if (accumulatedNormal.LengthSquared() < 1e-12f)
        {
            normal = Vector3.Zero;
            return false; // pontos colineares/degenerados: não dá para definir um plano
        }

        normal = Vector3.Normalize(accumulatedNormal);
        return true;
    }

    private static bool IsPlanar(IReadOnlyList<Vector3> points, Vector3 centroid, Vector3 normal)
    {
        var extent = 0f;
        foreach (var point in points)
        {
            extent = MathF.Max(extent, Vector3.Distance(point, centroid));
        }

        var tolerance = MathF.Max(extent * PlanarToleranceRatio, MinPlanarTolerance);

        foreach (var point in points)
        {
            if (MathF.Abs(Vector3.Dot(point - centroid, normal)) > tolerance)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Base ortonormal (u, v) do plano tal que u×v = normal — garante que a triangulação 2D
    /// (sempre CCW) produza triângulos com a normal 3D apontando para o mesmo lado escolhido aqui.</summary>
    private static (Vector3 U, Vector3 V) BuildPlaneBasis(Vector3 normal)
    {
        var reference = MathF.Abs(normal.Z) < 0.9f ? Vector3.UnitZ : Vector3.UnitX;
        var u = Vector3.Normalize(Vector3.Cross(reference, normal));
        var v = Vector3.Cross(normal, u);
        return (u, v);
    }
}
