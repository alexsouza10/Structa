using System.Numerics;

namespace Structa.Geometry.Faces;

/// <summary>
/// Agrupa triângulos coplanares e conectados por aresta, a partir de um triângulo semente — o
/// conceito de "face" que o Push/Pull opera, já que uma face desenhada (ex.: um quadrado) pode ter
/// mais de um triângulo internamente (Etapa 06). Puxar qualquer um deles deve mover a face inteira.
/// </summary>
public static class FaceGroupFinder
{
    // ~2.5 graus de tolerância entre normais para considerar dois triângulos "no mesmo plano".
    private const float PlanarDotTolerance = 0.999f;

    public static Vector3 TriangleNormal(Mesh mesh, int triangleIndex)
    {
        var (a, b, c) = mesh.Triangles[triangleIndex];
        return Vector3.Normalize(Vector3.Cross(mesh.Vertices[b] - mesh.Vertices[a], mesh.Vertices[c] - mesh.Vertices[a]));
    }

    /// <summary>Todos os triângulos alcançáveis do triângulo semente andando por arestas compartilhadas,
    /// sem nunca cruzar para um triângulo cuja normal diverge da do semente além da tolerância.</summary>
    public static List<int> FindConnectedCoplanarTriangles(Mesh mesh, int seedTriangleIndex)
    {
        var seedNormal = TriangleNormal(mesh, seedTriangleIndex);
        var adjacency = BuildTriangleAdjacency(mesh);

        var visited = new HashSet<int> { seedTriangleIndex };
        var queue = new Queue<int>();
        queue.Enqueue(seedTriangleIndex);
        var group = new List<int> { seedTriangleIndex };

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (!adjacency.TryGetValue(current, out var neighbors))
            {
                continue;
            }

            foreach (var neighbor in neighbors)
            {
                if (!visited.Add(neighbor))
                {
                    continue;
                }

                if (Vector3.Dot(seedNormal, TriangleNormal(mesh, neighbor)) < PlanarDotTolerance)
                {
                    continue; // vizinho existe, mas dobra para outro plano: não faz parte desta face
                }

                group.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }

        return group;
    }

    /// <summary>Índice de triângulo -&gt; triângulos vizinhos (que compartilham uma aresta com ele).</summary>
    private static Dictionary<int, List<int>> BuildTriangleAdjacency(Mesh mesh)
    {
        var edgeToTriangles = new Dictionary<(int, int), List<int>>();

        void RegisterEdge(int a, int b, int triangleIndex)
        {
            var key = a < b ? (a, b) : (b, a);
            if (!edgeToTriangles.TryGetValue(key, out var owners))
            {
                owners = [];
                edgeToTriangles[key] = owners;
            }

            owners.Add(triangleIndex);
        }

        for (var i = 0; i < mesh.Triangles.Count; i++)
        {
            var (a, b, c) = mesh.Triangles[i];
            RegisterEdge(a, b, i);
            RegisterEdge(b, c, i);
            RegisterEdge(c, a, i);
        }

        var adjacency = new Dictionary<int, List<int>>();

        foreach (var owners in edgeToTriangles.Values)
        {
            if (owners.Count < 2)
            {
                continue;
            }

            foreach (var t1 in owners)
            {
                foreach (var t2 in owners)
                {
                    if (t1 == t2)
                    {
                        continue;
                    }

                    if (!adjacency.TryGetValue(t1, out var neighbors))
                    {
                        neighbors = [];
                        adjacency[t1] = neighbors;
                    }

                    if (!neighbors.Contains(t2))
                    {
                        neighbors.Add(t2);
                    }
                }
            }
        }

        return adjacency;
    }
}
