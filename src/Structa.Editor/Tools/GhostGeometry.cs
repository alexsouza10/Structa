using System.Numerics;
using Structa.Geometry;

namespace Structa.Editor.Tools;

/// <summary>
/// Monta o "fantasma" (wireframe) de um subconjunto de vértices de uma malha depois de aplicar uma
/// transformação candidata, sem mutar nada — usado pelo preview de Mover, Rotacionar e Escalar (a
/// malha real só muda no commit). Só desenha arestas cujos dois vértices estão no subconjunto: uma
/// aresta parcialmente selecionada não tem um "fantasma" bem definido de qual lado se move.
/// </summary>
public static class GhostGeometry
{
    /// <summary>Pontos em pares consecutivos (ver <c>GhostOutlineRenderer.Render</c>), já transformados.</summary>
    public static List<Vector3> BuildTransformedEdgeSegments(Mesh mesh, IReadOnlySet<int> vertexIndices, Func<Vector3, Vector3> transform)
    {
        var segments = new List<Vector3>();

        foreach (var (a, b) in mesh.Edges)
        {
            if (!vertexIndices.Contains(a) || !vertexIndices.Contains(b))
            {
                continue;
            }

            segments.Add(transform(mesh.Vertices[a]));
            segments.Add(transform(mesh.Vertices[b]));
        }

        return segments;
    }
}
