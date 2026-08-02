using System.Numerics;
using Structa.Core.Selection;
using Structa.Geometry;

namespace Structa.Selection.Tests;

public class SelectionVertexResolverTests
{
    private static Mesh CreateTriangleMesh()
    {
        var mesh = new Mesh("Teste");
        mesh.AddVertex(new Vector3(0f, 0f, 0f));
        mesh.AddVertex(new Vector3(1f, 0f, 0f));
        mesh.AddVertex(new Vector3(0f, 1f, 0f));
        mesh.AddEdge(0, 1);
        mesh.AddEdge(1, 2);
        mesh.AddEdge(2, 0);
        mesh.AddTriangle(0, 1, 2);
        return mesh;
    }

    [Fact]
    public void Resolve_VertexSelection_ReturnsOnlyThatVertex()
    {
        var mesh = CreateTriangleMesh();
        HashSet<SelectableElement> selection = [new SelectableElement(mesh.Id, SelectionMode.Vertex, 1)];

        var result = SelectionVertexResolver.Resolve(selection, [mesh]);

        Assert.Equal([1], result[mesh.Id]);
    }

    [Fact]
    public void Resolve_EdgeSelection_ReturnsBothEndpoints()
    {
        var mesh = CreateTriangleMesh();
        HashSet<SelectableElement> selection = [new SelectableElement(mesh.Id, SelectionMode.Edge, 0)]; // aresta (0,1)

        var result = SelectionVertexResolver.Resolve(selection, [mesh]);

        Assert.Equal(new HashSet<int> { 0, 1 }, result[mesh.Id]);
    }

    [Fact]
    public void Resolve_FaceSelection_ReturnsAllThreeTriangleVertices()
    {
        var mesh = CreateTriangleMesh();
        HashSet<SelectableElement> selection = [new SelectableElement(mesh.Id, SelectionMode.Face, 0)];

        var result = SelectionVertexResolver.Resolve(selection, [mesh]);

        Assert.Equal(new HashSet<int> { 0, 1, 2 }, result[mesh.Id]);
    }

    [Fact]
    public void Resolve_ObjectSelection_ReturnsEveryVertexInTheMesh()
    {
        var mesh = CreateTriangleMesh();
        HashSet<SelectableElement> selection = [new SelectableElement(mesh.Id, SelectionMode.Object, 0)];

        var result = SelectionVertexResolver.Resolve(selection, [mesh]);

        Assert.Equal(new HashSet<int> { 0, 1, 2 }, result[mesh.Id]);
    }

    [Fact]
    public void Resolve_SelectionAcrossMultipleMeshes_KeepsEachMeshSeparate()
    {
        var meshA = CreateTriangleMesh();
        var meshB = CreateTriangleMesh();
        HashSet<SelectableElement> selection =
        [
            new SelectableElement(meshA.Id, SelectionMode.Vertex, 0),
            new SelectableElement(meshB.Id, SelectionMode.Vertex, 2),
        ];

        var result = SelectionVertexResolver.Resolve(selection, [meshA, meshB]);

        Assert.Equal([0], result[meshA.Id]);
        Assert.Equal([2], result[meshB.Id]);
    }

    [Fact]
    public void ComputeCentroid_AveragesVerticesAcrossAllAffectedMeshes()
    {
        var meshA = new Mesh("A");
        meshA.AddVertex(new Vector3(0f, 0f, 0f));
        meshA.AddVertex(new Vector3(2f, 0f, 0f));

        var meshB = new Mesh("B");
        meshB.AddVertex(new Vector3(4f, 0f, 0f));

        Dictionary<Guid, HashSet<int>> byMesh = new()
        {
            [meshA.Id] = [0, 1],
            [meshB.Id] = [0],
        };

        var centroid = SelectionVertexResolver.ComputeCentroid(byMesh, [meshA, meshB]);

        // (0 + 2 + 4) / 3 = 2
        Assert.True(Vector3.Distance(new Vector3(2f, 0f, 0f), centroid) < 1e-4f);
    }
}
