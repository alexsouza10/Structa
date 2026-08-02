using System.Numerics;

namespace Structa.Selection;

/// <summary>
/// Inferência de eixo (X/Y/Z) a partir de um ponto de origem: acha, entre os três eixos do mundo
/// partindo desse ponto, qual passa mais perto do cursor em pixels de tela — e "gruda" nele se estiver
/// dentro da tolerância. É a mesma inferência vermelho/verde/azul do SketchUp, usada pela ferramenta
/// Linha (Etapa 05) e por Mover (Etapa 08).
/// </summary>
public static class AxisSnapper
{
    private static readonly Vector3[] Axes = [Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ];

    /// <summary><paramref name="axisIndex"/>: 0=X, 1=Y, 2=Z.</summary>
    public static bool TryFindClosestAxis(
        Ray ray,
        Vector3 origin,
        Vector2 screenPoint,
        Vector2 viewportSize,
        Matrix4x4 view,
        Matrix4x4 projection,
        float pixelTolerance,
        out Vector3 point,
        out int axisIndex)
    {
        var bestDistance = pixelTolerance;
        point = default;
        axisIndex = -1;
        var found = false;

        for (var i = 0; i < Axes.Length; i++)
        {
            if (!RayIntersection.TryClosestPointOnLine(ray, origin, Axes[i], out var candidate) ||
                !RayCaster.TryWorldToScreen(candidate, viewportSize, view, projection, out var screen))
            {
                continue;
            }

            var distance = Vector2.Distance(screenPoint, screen);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                point = candidate;
                axisIndex = i;
                found = true;
            }
        }

        return found;
    }
}
