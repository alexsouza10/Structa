using System.Numerics;
using Structa.Geometry;

namespace Structa.Geometry.Tests;

public class MeshTests
{
    [Fact]
    public void AddVertex_ReturnsSequentialIndicesAndBumpsVersion()
    {
        var mesh = new Mesh("Teste");

        var first = mesh.AddVertex(new Vector3(0f, 0f, 0f));
        var second = mesh.AddVertex(new Vector3(1f, 0f, 0f));

        Assert.Equal(0, first);
        Assert.Equal(1, second);
        Assert.Equal(2, mesh.Vertices.Count);
        Assert.Equal(2, mesh.Version);
    }

    [Fact]
    public void AddEdge_BumpsVersionAndAppearsInEdges()
    {
        var mesh = new Mesh("Teste");
        var a = mesh.AddVertex(Vector3.Zero);
        var b = mesh.AddVertex(Vector3.UnitX);
        var versionBeforeEdge = mesh.Version;

        mesh.AddEdge(a, b);

        Assert.Equal(versionBeforeEdge + 1, mesh.Version);
        Assert.Contains((a, b), mesh.Edges);
    }

    [Theory]
    [InlineData(0, 1, 0, 1)]
    [InlineData(0, 1, 1, 0)]
    public void HasEdge_IsDirectionAgnostic(int a, int b, int queryA, int queryB)
    {
        var mesh = new Mesh("Teste");
        mesh.AddVertex(Vector3.Zero);
        mesh.AddVertex(Vector3.UnitX);
        mesh.AddEdge(a, b);

        Assert.True(mesh.HasEdge(queryA, queryB));
    }

    [Fact]
    public void HasEdge_ReturnsFalseForUnrelatedPair()
    {
        var mesh = new Mesh("Teste");
        mesh.AddVertex(Vector3.Zero);
        mesh.AddVertex(Vector3.UnitX);
        mesh.AddVertex(Vector3.UnitY);
        mesh.AddEdge(0, 1);

        Assert.False(mesh.HasEdge(0, 2));
    }

    [Fact]
    public void AddTriangle_BumpsVersionAndAppearsInTriangles()
    {
        var mesh = new Mesh("Teste");
        mesh.AddVertex(Vector3.Zero);
        mesh.AddVertex(Vector3.UnitX);
        mesh.AddVertex(Vector3.UnitY);
        var versionBeforeTriangle = mesh.Version;

        mesh.AddTriangle(0, 1, 2);

        Assert.Equal(versionBeforeTriangle + 1, mesh.Version);
        Assert.Contains((0, 1, 2), mesh.Triangles);
    }

    [Theory]
    [InlineData(0, 1, 2, 0, 1, 2)]
    [InlineData(0, 1, 2, 1, 2, 0)]
    [InlineData(0, 1, 2, 2, 1, 0)]
    public void HasTriangle_IsAgnosticToVertexOrder(int a, int b, int c, int queryA, int queryB, int queryC)
    {
        var mesh = new Mesh("Teste");
        mesh.AddVertex(Vector3.Zero);
        mesh.AddVertex(Vector3.UnitX);
        mesh.AddVertex(Vector3.UnitY);
        mesh.AddTriangle(a, b, c);

        Assert.True(mesh.HasTriangle(queryA, queryB, queryC));
    }

    [Fact]
    public void HasTriangle_ReturnsFalseForUnrelatedVertexSet()
    {
        var mesh = new Mesh("Teste");
        mesh.AddVertex(Vector3.Zero);
        mesh.AddVertex(Vector3.UnitX);
        mesh.AddVertex(Vector3.UnitY);
        mesh.AddVertex(Vector3.UnitZ);
        mesh.AddTriangle(0, 1, 2);

        Assert.False(mesh.HasTriangle(0, 1, 3));
    }

    [Fact]
    public void Constructor_AcceptsInitialGeometry()
    {
        Vector3[] vertices = [Vector3.Zero, Vector3.UnitX, Vector3.UnitY];
        (int, int)[] edges = [(0, 1), (1, 2)];
        (int, int, int)[] triangles = [(0, 1, 2)];

        var mesh = new Mesh("Primitivo", vertices, edges, triangles);

        Assert.Equal(3, mesh.Vertices.Count);
        Assert.Equal(2, mesh.Edges.Count);
        Assert.Single(mesh.Triangles);
        Assert.Equal(0, mesh.Version);
    }
}
