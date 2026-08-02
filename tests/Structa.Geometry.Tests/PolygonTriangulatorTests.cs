using System.Numerics;
using Structa.Geometry.Faces;

namespace Structa.Geometry.Tests;

public class PolygonTriangulatorTests
{
    [Fact]
    public void Triangulate_Triangle_ReturnsTheTriangleItself()
    {
        Vector2[] polygon = [new(0f, 0f), new(2f, 0f), new(0f, 2f)];

        var triangles = PolygonTriangulator.Triangulate(polygon);

        var triangle = Assert.Single(triangles);
        Assert.Equal((0, 1, 2), triangle);
    }

    [Fact]
    public void Triangulate_ConvexQuad_ReturnsTwoTrianglesCoveringTheFullArea()
    {
        Vector2[] polygon = [new(0f, 0f), new(1f, 0f), new(1f, 1f), new(0f, 1f)];

        var triangles = PolygonTriangulator.Triangulate(polygon);

        Assert.Equal(2, triangles.Count);
        AssertCoversArea(polygon, triangles, expectedArea: 1f);
    }

    [Fact]
    public void Triangulate_ConcaveLShape_CoversTheFullPolygonArea()
    {
        // "L" de 2x2 com um quadrado 1x1 recortado do canto superior direito — área = 3.
        Vector2[] polygon =
        [
            new(0f, 0f), new(2f, 0f), new(2f, 1f), new(1f, 1f), new(1f, 2f), new(0f, 2f),
        ];

        var triangles = PolygonTriangulator.Triangulate(polygon);

        Assert.Equal(polygon.Length - 2, triangles.Count);
        AssertCoversArea(polygon, triangles, expectedArea: 3f);
    }

    private static void AssertCoversArea(IReadOnlyList<Vector2> polygon, List<(int A, int B, int C)> triangles, float expectedArea)
    {
        var total = 0f;
        foreach (var (a, b, c) in triangles)
        {
            var ab = polygon[b] - polygon[a];
            var ac = polygon[c] - polygon[a];
            total += MathF.Abs((ab.X * ac.Y) - (ab.Y * ac.X)) / 2f;
        }

        Assert.True(MathF.Abs(total - expectedArea) < 1e-3f, $"área esperada {expectedArea}, obtida {total}");
    }
}
