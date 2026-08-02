using System.Numerics;

namespace Structa.Geometry.Faces;

/// <summary>
/// Extrusão de um grupo de faces coplanares (<see cref="FaceGroupFinder"/>) ao longo da normal — o
/// "Push Pull" da Etapa 07. Cada vértice do grupo é tratado de um dos dois jeitos, dependendo de já
/// pertencer a mais alguma coisa na malha:
///
/// <list type="bullet">
/// <item>Vértice <b>exclusivo</b> da face (não usado por nenhum triângulo fora do grupo): a face está
/// "solta" ali, então o vértice original vira a base (fica parado) e um vértice novo é criado no topo —
/// isso fecha o sólido criando uma tampa nova e paredes conectando as duas.</item>
/// <item>Vértice <b>compartilhado</b> (já usado por outro triângulo, ex.: a lateral de uma caixa): esse
/// vértice é só deslocado no lugar — a geometria vizinha, que referencia o mesmo índice, estica junto
/// sozinha, sem precisar criar nada novo. É assim que puxar o topo de uma caixa simplesmente a deixa
/// mais alta, sem duplicar geometria.</item>
/// </list>
///
/// Limitação conhecida: se um único vértice do contorno for compartilhado através de uma aresta
/// diferente da que está sendo processada (topologia não-manifold rara), a parede daquele trecho é
/// pulada em vez de arriscar geometria incorreta — não ocorre com a geometria que as ferramentas atuais
/// produzem (caixa primitiva e faces soltas desenhadas pela ferramenta Linha).
/// </summary>
public static class FaceExtruder
{
    private const float MinDistance = 1e-6f;

    public static bool Extrude(Mesh mesh, IReadOnlyList<int> faceTriangleIndices, Vector3 normal, float distance)
    {
        if (faceTriangleIndices.Count == 0 || MathF.Abs(distance) < MinDistance)
        {
            return false;
        }

        var offset = normal * distance;
        var groupSet = new HashSet<int>(faceTriangleIndices);

        var groupVertices = new HashSet<int>();
        foreach (var t in faceTriangleIndices)
        {
            var (a, b, c) = mesh.Triangles[t];
            groupVertices.Add(a);
            groupVertices.Add(b);
            groupVertices.Add(c);
        }

        // Tudo que precisa da topologia original (quem é exclusivo, o contorno) é resolvido ANTES de
        // qualquer mutação — depois que começarmos a reatribuir triângulos do grupo, mesh.Triangles não
        // reflete mais a forma original.
        var exclusive = groupVertices.ToDictionary(v => v, v => IsExclusiveToGroup(mesh, groupSet, v));
        var boundary = FaceBoundary.FindDirectedBoundaryEdges(mesh, faceTriangleIndices);
        var wallNeeded = boundary.ToDictionary(edge => edge, edge => !HasExternalTriangleUsingEdge(mesh, groupSet, edge.From, edge.To));

        var originalPositions = groupVertices.ToDictionary(v => v, v => mesh.Vertices[v]);

        var topIndex = new Dictionary<int, int>();
        foreach (var v in groupVertices)
        {
            topIndex[v] = exclusive[v]
                ? mesh.AddVertex(originalPositions[v] + offset)
                : MoveInPlace(mesh, v, originalPositions[v] + offset);
        }

        foreach (var t in faceTriangleIndices)
        {
            var (a, b, c) = mesh.Triangles[t];

            if (exclusive[a] && exclusive[b] && exclusive[c])
            {
                // Face solta: o triângulo original vira a tampa de baixo (giro invertido, agora vira
                // para fora do novo sólido) e uma cópia nos vértices do topo vira a tampa de cima.
                mesh.ReplaceTriangle(t, a, c, b);
                mesh.AddTriangle(topIndex[a], topIndex[b], topIndex[c]);
            }
            else
            {
                // Já fazia parte de um sólido (ex.: topo de uma caixa): só reposiciona para o topo.
                mesh.ReplaceTriangle(t, topIndex[a], topIndex[b], topIndex[c]);
            }
        }

        foreach (var edge in boundary)
        {
            if (!wallNeeded[edge])
            {
                continue; // já existe geometria do outro lado dessa aresta — ela estica sozinha
            }

            var (bottom1, bottom2) = edge;
            var top1 = topIndex[bottom1];
            var top2 = topIndex[bottom2];

            if (bottom1 == top1 || bottom2 == top2)
            {
                continue; // vértice compartilhado sem "base" própria nesta aresta — ver limitação acima
            }

            AddTriangleIfNew(mesh, bottom1, bottom2, top2);
            AddTriangleIfNew(mesh, bottom1, top2, top1);

            EnsureEdge(mesh, top1, top2);
            EnsureEdge(mesh, bottom1, top1);
            EnsureEdge(mesh, bottom2, top2);
        }

        return true;
    }

    private static int MoveInPlace(Mesh mesh, int index, Vector3 position)
    {
        mesh.MoveVertex(index, position);
        return index;
    }

    private static void AddTriangleIfNew(Mesh mesh, int a, int b, int c)
    {
        if (!mesh.HasTriangle(a, b, c))
        {
            mesh.AddTriangle(a, b, c);
        }
    }

    private static void EnsureEdge(Mesh mesh, int a, int b)
    {
        if (!mesh.HasEdge(a, b))
        {
            mesh.AddEdge(a, b);
        }
    }

    private static bool IsExclusiveToGroup(Mesh mesh, HashSet<int> groupSet, int vertexIndex)
    {
        for (var i = 0; i < mesh.Triangles.Count; i++)
        {
            if (groupSet.Contains(i))
            {
                continue;
            }

            var (a, b, c) = mesh.Triangles[i];
            if (a == vertexIndex || b == vertexIndex || c == vertexIndex)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasExternalTriangleUsingEdge(Mesh mesh, HashSet<int> groupSet, int v1, int v2)
    {
        for (var i = 0; i < mesh.Triangles.Count; i++)
        {
            if (groupSet.Contains(i))
            {
                continue;
            }

            var (a, b, c) = mesh.Triangles[i];
            var usesV1 = a == v1 || b == v1 || c == v1;
            var usesV2 = a == v2 || b == v2 || c == v2;

            if (usesV1 && usesV2)
            {
                return true;
            }
        }

        return false;
    }
}
