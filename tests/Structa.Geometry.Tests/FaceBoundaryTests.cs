using Structa.Geometry.Faces;

namespace Structa.Geometry.Tests;

public class FaceBoundaryTests
{
    [Fact]
    public void FindDirectedBoundaryEdges_SingleTriangle_ReturnsAllThreeEdges()
    {
        var mesh = new Mesh("Teste");
        mesh.AddVertex(default);
        mesh.AddVertex(default);
        mesh.AddVertex(default);
        mesh.AddTriangle(0, 1, 2);

        var boundary = FaceBoundary.FindDirectedBoundaryEdges(mesh, [0]);

        Assert.Equal(3, boundary.Count);
        Assert.Contains((0, 1), boundary);
        Assert.Contains((1, 2), boundary);
        Assert.Contains((2, 0), boundary);
    }

    [Fact]
    public void FindDirectedBoundaryEdges_TwoTrianglesSharingAnEdge_CancelsTheSharedDiagonal()
    {
        var mesh = new Mesh("Teste");
        for (var i = 0; i < 4; i++)
        {
            mesh.AddVertex(default);
        }

        mesh.AddTriangle(0, 1, 2);
        mesh.AddTriangle(0, 2, 3); // compartilha a aresta 0-2 (diagonal) com o primeiro

        var boundary = FaceBoundary.FindDirectedBoundaryEdges(mesh, [0, 1]);

        Assert.Equal(4, boundary.Count);
        Assert.DoesNotContain((0, 2), boundary);
        Assert.DoesNotContain((2, 0), boundary);
        Assert.Contains((0, 1), boundary);
        Assert.Contains((1, 2), boundary);
        Assert.Contains((2, 3), boundary);
        Assert.Contains((3, 0), boundary);
    }

    [Fact]
    public void TryOrderLoop_QuadBoundary_ReturnsASingleOrderedCycle()
    {
        List<(int From, int To)> edges = [(2, 3), (0, 1), (3, 0), (1, 2)];

        var ok = FaceBoundary.TryOrderLoop(edges, out var loop);

        Assert.True(ok);
        Assert.Equal(4, loop.Count);
        // O loop pode começar em qualquer vértice, mas precisa manter a sequência circular 0-1-2-3.
        var startIndex = loop.IndexOf(0);
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(i, loop[(startIndex + i) % 4]);
        }
    }

    [Fact]
    public void TryOrderLoop_EmptyEdgeList_ReturnsFalse()
    {
        var ok = FaceBoundary.TryOrderLoop([], out var loop);

        Assert.False(ok);
        Assert.Empty(loop);
    }
}
