using System.Linq;
using System.Numerics;
using Structa.Core.Selection;
using Structa.Geometry;

namespace Structa.Selection;

/// <summary>
/// Traduz um conjunto de <see cref="SelectableElement"/> (vértices, arestas, faces ou objetos
/// selecionados, possivelmente espalhados por várias malhas) para os índices de vértice realmente
/// afetados em cada malha — o que ferramentas de transformação (Mover, Rotacionar, Escalar, Espelhar)
/// precisam para saber o que mexer. Uma aresta ou face selecionada afeta os vértices que ela usa; um
/// objeto selecionado afeta todos os vértices da malha.
/// </summary>
public static class SelectionVertexResolver
{
    public static Dictionary<Guid, HashSet<int>> Resolve(IReadOnlySet<SelectableElement> selection, IReadOnlyList<Mesh> meshes)
    {
        var result = new Dictionary<Guid, HashSet<int>>();

        foreach (var element in selection)
        {
            var mesh = meshes.FirstOrDefault(m => m.Id == element.MeshId);
            if (mesh is null)
            {
                continue;
            }

            var bucket = GetBucket(result, mesh.Id);

            switch (element.Kind)
            {
                case SelectionMode.Vertex:
                    bucket.Add(element.Index);
                    break;

                case SelectionMode.Edge:
                    var (a, b) = mesh.Edges[element.Index];
                    bucket.Add(a);
                    bucket.Add(b);
                    break;

                case SelectionMode.Face:
                    var (ta, tb, tc) = mesh.Triangles[element.Index];
                    bucket.Add(ta);
                    bucket.Add(tb);
                    bucket.Add(tc);
                    break;

                case SelectionMode.Object:
                    for (var i = 0; i < mesh.Vertices.Count; i++)
                    {
                        bucket.Add(i);
                    }

                    break;
            }
        }

        return result;
    }

    /// <summary>Centroide combinado de todos os vértices afetados, possivelmente espalhados por várias
    /// malhas — o pivô que Mover, Rotacionar e Escalar usam por padrão.</summary>
    public static Vector3 ComputeCentroid(Dictionary<Guid, HashSet<int>> verticesByMesh, IReadOnlyList<Mesh> meshes)
    {
        var sum = Vector3.Zero;
        var count = 0;

        foreach (var (meshId, vertices) in verticesByMesh)
        {
            var mesh = meshes.FirstOrDefault(m => m.Id == meshId);
            if (mesh is null)
            {
                continue;
            }

            foreach (var v in vertices)
            {
                sum += mesh.Vertices[v];
                count++;
            }
        }

        return count == 0 ? Vector3.Zero : sum / count;
    }

    private static HashSet<int> GetBucket(Dictionary<Guid, HashSet<int>> result, Guid meshId)
    {
        if (!result.TryGetValue(meshId, out var bucket))
        {
            bucket = [];
            result[meshId] = bucket;
        }

        return bucket;
    }
}
