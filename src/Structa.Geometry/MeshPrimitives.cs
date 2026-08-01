using System.Numerics;

namespace Structa.Geometry;

/// <summary>
/// Fábricas de malhas simples. Servem hoje apenas como conteúdo de teste para validar o sistema
/// de seleção (Etapa 04) — a criação de geometria pelo usuário chega nas Etapas 05/06 (ferramentas
/// de desenho e detecção automática de faces).
/// </summary>
public static class MeshPrimitives
{
    public static Mesh CreateBox(string name, Vector3 center, float size)
    {
        var h = size / 2f;

        Vector3[] vertices =
        [
            center + new Vector3(-h, -h, -h), // 0
            center + new Vector3(h, -h, -h), // 1
            center + new Vector3(h, h, -h), // 2
            center + new Vector3(-h, h, -h), // 3
            center + new Vector3(-h, -h, h), // 4
            center + new Vector3(h, -h, h), // 5
            center + new Vector3(h, h, h), // 6
            center + new Vector3(-h, h, h), // 7
        ];

        (int, int)[] edges =
        [
            (0, 1), (1, 2), (2, 3), (3, 0), // face inferior
            (4, 5), (5, 6), (6, 7), (7, 4), // face superior
            (0, 4), (1, 5), (2, 6), (3, 7), // verticais
        ];

        (int, int, int)[] triangles =
        [
            (0, 3, 2), (0, 2, 1), // inferior
            (4, 5, 6), (4, 6, 7), // superior
            (0, 1, 5), (0, 5, 4), // frente
            (2, 3, 7), (2, 7, 6), // fundo
            (0, 4, 7), (0, 7, 3), // esquerda
            (1, 2, 6), (1, 6, 5), // direita
        ];

        return new Mesh
        {
            Name = name,
            Vertices = vertices,
            Edges = edges,
            Triangles = triangles,
        };
    }

    /// <summary>
    /// Malha plana com contorno lobado (folha de acanto estilizada), deitada no plano XY com uma
    /// nervura central em relevo. Serve para validar seleção/renderização com um contorno côncavo e
    /// número de vértices maior que os primitivos simples — a triangulação usa "ear clipping" porque,
    /// ao contrário do box, o polígono não é convexo.
    /// </summary>
    public static Mesh CreateAcanthusLeaf(string name, Vector3 origin, float length = 2.4f)
    {
        var outline = BuildAcanthusOutline(length);
        var triangles = TriangulatePolygon(outline);

        var vertices = new Vector3[outline.Count];
        for (var i = 0; i < outline.Count; i++)
        {
            var point = outline[i];
            vertices[i] = origin + new Vector3(point.X, point.Y, RidgeRelief(point.X, length));
        }

        var edges = new (int, int)[outline.Count];
        for (var i = 0; i < outline.Count; i++)
        {
            edges[i] = (i, (i + 1) % outline.Count);
        }

        return new Mesh
        {
            Name = name,
            Vertices = vertices,
            Edges = edges,
            Triangles = triangles,
        };
    }

    private const int AcanthusLobesPerSide = 6;

    /// <summary>
    /// Contorno 2D (X = largura lateral, Y = comprimento a partir da base) de meia-folha espelhada:
    /// base -> lóbulos alternando ponta/reentrância até a ponta -> lóbulos espelhados de volta à base.
    /// </summary>
    private static List<Vector2> BuildAcanthusOutline(float length)
    {
        var maxHalfWidth = length * 0.22f;
        float Envelope(float y) => maxHalfWidth * MathF.Sin(MathF.PI * y / length);

        var points = new List<Vector2> { new(0f, 0f) };

        for (var i = 1; i <= AcanthusLobesPerSide; i++)
        {
            var yStart = length * (i - 1) / AcanthusLobesPerSide;
            var yEnd = length * i / AcanthusLobesPerSide;
            var yTip = yStart + (0.55f * (yEnd - yStart));

            points.Add(new Vector2(Envelope(yTip), yTip)); // ponta do lóbulo
            points.Add(new Vector2(Envelope(yEnd) * 0.2f, yEnd)); // reentrância perto da nervura
        }

        // Espelha o lado direito de volta até (sem repetir) a base, fechando o contorno.
        for (var i = points.Count - 2; i >= 1; i--)
        {
            points.Add(new Vector2(-points[i].X, points[i].Y));
        }

        return points;
    }

    private static float RidgeRelief(float lateralOffset, float length)
    {
        var ridgeHeight = length * 0.035f;
        var ridgeSigma = length * 0.05f;
        var normalized = lateralOffset / ridgeSigma;
        return ridgeHeight * MathF.Exp(-0.5f * normalized * normalized);
    }

    /// <summary>Triangulação por "ear clipping" de um polígono simples (côncavo permitido, sem furos).</summary>
    private static List<(int A, int B, int C)> TriangulatePolygon(IReadOnlyList<Vector2> polygon)
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
