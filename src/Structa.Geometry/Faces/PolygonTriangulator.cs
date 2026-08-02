using System.Numerics;

namespace Structa.Geometry.Faces;

/// <summary>
/// Triangulação por "ear clipping" de um polígono 2D simples (côncavo permitido, sem furos). Os
/// índices retornados são posições dentro de <c>polygon</c> (0-based) — quem chama é responsável por
/// mapear de volta para índices de vértice reais, se o polígono já era uma projeção de outra coisa
/// (ex.: <see cref="FaceDetector"/> projeta um loop 3D para 2D antes de triangular).
/// </summary>
public static class PolygonTriangulator
{
    public static List<(int A, int B, int C)> Triangulate(IReadOnlyList<Vector2> polygon)
    {
        var order = new List<int>(polygon.Count);
        for (var i = 0; i < polygon.Count; i++)
        {
            order.Add(i);
        }

        if (SignedArea(polygon) < 0f)
        {
            order.Reverse();
        }

        var triangles = new List<(int, int, int)>(polygon.Count - 2);
        var remainingAttempts = polygon.Count * polygon.Count;

        while (order.Count > 3 && remainingAttempts-- > 0)
        {
            var clipped = false;

            for (var i = 0; i < order.Count; i++)
            {
                var iPrev = order[(i - 1 + order.Count) % order.Count];
                var iCurr = order[i];
                var iNext = order[(i + 1) % order.Count];

                var a = polygon[iPrev];
                var b = polygon[iCurr];
                var c = polygon[iNext];

                if (Cross(b - a, c - b) <= 1e-8f)
                {
                    continue; // vértice côncavo (reflexo) ou degenerado: não é uma "orelha"
                }

                var isEar = true;
                foreach (var j in order)
                {
                    if (j == iPrev || j == iCurr || j == iNext)
                    {
                        continue;
                    }

                    if (PointInTriangle(polygon[j], a, b, c))
                    {
                        isEar = false;
                        break;
                    }
                }

                if (!isEar)
                {
                    continue;
                }

                triangles.Add((iPrev, iCurr, iNext));
                order.RemoveAt(i);
                clipped = true;
                break;
            }

            if (!clipped)
            {
                break; // fallback (leque) abaixo cobre o que restar em caso de degeneração numérica
            }
        }

        for (var i = 1; i < order.Count - 1; i++)
        {
            triangles.Add((order[0], order[i], order[i + 1]));
        }

        return triangles;
    }

    private static float Cross(Vector2 a, Vector2 b) => (a.X * b.Y) - (a.Y * b.X);

    private static float SignedArea(IReadOnlyList<Vector2> polygon)
    {
        var area = 0f;
        for (var i = 0; i < polygon.Count; i++)
        {
            var j = (i + 1) % polygon.Count;
            area += (polygon[i].X * polygon[j].Y) - (polygon[j].X * polygon[i].Y);
        }

        return area * 0.5f;
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        var d1 = Cross(p - a, b - a);
        var d2 = Cross(p - b, c - b);
        var d3 = Cross(p - c, a - c);

        var hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
        var hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;

        return !(hasNegative && hasPositive);
    }
}
