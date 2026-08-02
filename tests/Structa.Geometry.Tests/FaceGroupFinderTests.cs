using System.Numerics;
using Structa.Geometry.Faces;

namespace Structa.Geometry.Tests;

public class FaceGroupFinderTests
{
    [Fact]
    public void FindConnectedCoplanarTriangles_BoxTopFace_GroupsBothTrianglesOfThatFaceOnly()
    {
        var box = MeshPrimitives.CreateBox("Caixa", Vector3.Zero, 2f);

        // Índices 2 e 3 são os dois triângulos do topo (ver MeshPrimitives.CreateBox).
        var group = FaceGroupFinder.FindConnectedCoplanarTriangles(box, 2);

        Assert.Equal(2, group.Count);
        Assert.Contains(2, group);
        Assert.Contains(3, group);
    }

    [Fact]
    public void FindConnectedCoplanarTriangles_BoxFrontFace_DoesNotLeakIntoAdjacentFaces()
    {
        var box = MeshPrimitives.CreateBox("Caixa", Vector3.Zero, 2f);

        // Índices 4 e 5 são os dois triângulos da face da frente.
        var group = FaceGroupFinder.FindConnectedCoplanarTriangles(box, 4);

        Assert.Equal(2, group.Count);
        Assert.Contains(4, group);
        Assert.Contains(5, group);
    }

    [Fact]
    public void TriangleNormal_BoxTopFace_PointsUp()
    {
        var box = MeshPrimitives.CreateBox("Caixa", Vector3.Zero, 2f);

        var normal = FaceGroupFinder.TriangleNormal(box, 2);

        Assert.True(Vector3.Distance(normal, Vector3.UnitZ) < 1e-4f, $"normal esperada +Z, obtida {normal}");
    }
}
