namespace Structa.Geometry.Faces;

/// <summary>
/// Extrai o contorno de um grupo de triângulos coplanares conectados (<see cref="FaceGroupFinder"/>):
/// as arestas que pertencem a só um triângulo do grupo. Usado pelo <see cref="FaceExtruder"/> (para
/// saber onde criar paredes) e pelo preview do Push/Pull (para desenhar o contorno fantasma).
/// </summary>
public static class FaceBoundary
{
    /// <summary>
    /// Arestas de contorno, direcionadas na ordem de giro dos triângulos do grupo. Uma aresta interna
    /// (compartilhada por 2 triângulos do grupo) aparece uma vez em cada sentido a partir de cada
    /// triângulo — os dois sentidos se cancelam, sobrando só as arestas realmente na borda, já na
    /// ordem consistente de giro (a mesma técnica usada para achar a silhueta de uma malha).
    /// </summary>
    public static List<(int From, int To)> FindDirectedBoundaryEdges(Mesh mesh, IReadOnlyList<int> faceTriangleIndices)
    {
        var directed = new HashSet<(int From, int To)>();

        foreach (var t in faceTriangleIndices)
        {
            var (a, b, c) = mesh.Triangles[t];
            AddOrCancel(directed, a, b);
            AddOrCancel(directed, b, c);
            AddOrCancel(directed, c, a);
        }

        return [.. directed];
    }

    private static void AddOrCancel(HashSet<(int From, int To)> directed, int from, int to)
    {
        if (!directed.Remove((to, from)))
        {
            directed.Add((from, to));
        }
    }

    /// <summary>
    /// Encadeia as arestas de contorno direcionadas em um único loop fechado. A geometria atual
    /// (primitivos e faces desenhadas pela ferramenta Linha) sempre produz um contorno simples; retorna
    /// false se a topologia não for um loop único (ex.: face com furo), que fica fora do escopo atual.
    /// </summary>
    public static bool TryOrderLoop(IReadOnlyList<(int From, int To)> directedBoundaryEdges, out List<int> loop)
    {
        loop = [];

        if (directedBoundaryEdges.Count == 0)
        {
            return false;
        }

        var next = new Dictionary<int, int>();
        foreach (var (from, to) in directedBoundaryEdges)
        {
            if (!next.TryAdd(from, to))
            {
                return false; // mais de uma aresta de contorno saindo do mesmo vértice: não é um loop simples
            }
        }

        var start = directedBoundaryEdges[0].From;
        var current = start;

        do
        {
            loop.Add(current);
            if (!next.TryGetValue(current, out current))
            {
                return false;
            }
        }
        while (current != start && loop.Count <= next.Count);

        return current == start && loop.Count == next.Count;
    }
}
