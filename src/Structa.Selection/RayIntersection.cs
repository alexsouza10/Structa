using System.Numerics;

namespace Structa.Selection;

/// <summary>Testes de interseção usados pelo picking.</summary>
public static class RayIntersection
{
    private const float Epsilon = 1e-6f;

    /// <summary>Interseção raio-triângulo (algoritmo de Möller–Trumbore).</summary>
    public static bool TryIntersectTriangle(Ray ray, Vector3 a, Vector3 b, Vector3 c, out float distance)
    {
        distance = 0f;

        var edge1 = b - a;
        var edge2 = c - a;
        var pVec = Vector3.Cross(ray.Direction, edge2);
        var det = Vector3.Dot(edge1, pVec);

        if (MathF.Abs(det) < Epsilon)
        {
            return false;
        }

        var invDet = 1f / det;
        var tVec = ray.Origin - a;
        var u = Vector3.Dot(tVec, pVec) * invDet;

        if (u is < 0f or > 1f)
        {
            return false;
        }

        var qVec = Vector3.Cross(tVec, edge1);
        var v = Vector3.Dot(ray.Direction, qVec) * invDet;

        if (v < 0f || u + v > 1f)
        {
            return false;
        }

        var t = Vector3.Dot(edge2, qVec) * invDet;
        if (t < Epsilon)
        {
            return false;
        }

        distance = t;
        return true;
    }

    /// <summary>Distância (em pixels) de um ponto até o segmento de reta 2D mais próximo.</summary>
    public static float PointToSegmentDistance(Vector2 point, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var lengthSquared = ab.LengthSquared();

        if (lengthSquared < 1e-9f)
        {
            return Vector2.Distance(point, a);
        }

        var t = Math.Clamp(Vector2.Dot(point - a, ab) / lengthSquared, 0f, 1f);
        var closest = a + (ab * t);

        return Vector2.Distance(point, closest);
    }
}
