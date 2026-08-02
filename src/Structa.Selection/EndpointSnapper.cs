using System.Numerics;
using Structa.Geometry;

namespace Structa.Selection;

/// <summary>
/// Encontra o vértice de qualquer malha da cena mais próximo, em pixels de tela, de um ponto de
/// referência. Usado pelo <see cref="SelectionManager"/> (picking em modo Vértice) e por ferramentas
/// de desenho como a de Linha (snap em pontas de arestas existentes).
/// </summary>
public static class EndpointSnapper
{
    public static bool TryFindClosest(
        Vector2 screenPoint,
        IReadOnlyList<Mesh> meshes,
        Vector2 viewportSize,
        Matrix4x4 view,
        Matrix4x4 projection,
        float pixelTolerance,
        out Guid meshId,
        out int vertexIndex,
        out Vector3 position)
    {
        meshId = default;
        vertexIndex = -1;
        position = default;

        var bestDistance = pixelTolerance;
        var found = false;

        foreach (var mesh in meshes)
        {
            for (var i = 0; i < mesh.Vertices.Count; i++)
            {
                if (!RayCaster.TryWorldToScreen(mesh.Vertices[i], viewportSize, view, projection, out var screen))
                {
                    continue;
                }

                var distance = Vector2.Distance(screenPoint, screen);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    meshId = mesh.Id;
                    vertexIndex = i;
                    position = mesh.Vertices[i];
                    found = true;
                }
            }
        }

        return found;
    }
}
