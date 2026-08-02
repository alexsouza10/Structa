using System.Numerics;
using Structa.Geometry.Faces;

namespace Structa.Geometry;

/// <summary>
/// Fábricas de malhas simples. Servem hoje apenas como conteúdo de teste para validar o sistema
/// de seleção (Etapa 04) e a detecção automática de faces (Etapa 06) — a criação de geometria pelo
/// usuário via ferramentas de desenho chegou na Etapa 05.
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

        return new Mesh(name, vertices, edges, triangles);
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
        var triangles = PolygonTriangulator.Triangulate(outline);

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

        return new Mesh(name, vertices, edges, triangles);
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
}
