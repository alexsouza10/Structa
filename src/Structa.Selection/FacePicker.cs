using System.Numerics;
using Structa.Geometry;

namespace Structa.Selection;

/// <summary>
/// Encontra o triângulo mais próximo, ao longo de um raio, entre todas as malhas da cena. Usado pelo
/// <see cref="SelectionManager"/> (picking em modo Face) e pelo <c>PushPullTool</c> (escolher em qual
/// face começar a extrusão).
/// </summary>
public static class FacePicker
{
    public static bool TryFindClosest(
        Ray ray,
        IReadOnlyList<Mesh> meshes,
        out Guid meshId,
        out int triangleIndex,
        out Vector3 hitPoint)
    {
        meshId = default;
        triangleIndex = -1;
        hitPoint = default;

        var bestDistance = float.MaxValue;
        var found = false;

        foreach (var mesh in meshes)
        {
            for (var i = 0; i < mesh.Triangles.Count; i++)
            {
                var (a, b, c) = mesh.Triangles[i];

                if (RayIntersection.TryIntersectTriangle(ray, mesh.Vertices[a], mesh.Vertices[b], mesh.Vertices[c], out var distance)
                    && distance < bestDistance)
                {
                    bestDistance = distance;
                    meshId = mesh.Id;
                    triangleIndex = i;
                    found = true;
                }
            }
        }

        if (found)
        {
            hitPoint = ray.Origin + (ray.Direction * bestDistance);
        }

        return found;
    }
}
