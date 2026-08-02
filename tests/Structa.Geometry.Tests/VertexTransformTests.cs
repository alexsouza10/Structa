using System.Numerics;
using Structa.Geometry.Transform;

namespace Structa.Geometry.Tests;

public class VertexTransformTests
{
    [Fact]
    public void Translate_MovesTheGivenVerticesByDelta()
    {
        var mesh = new Mesh("Teste");
        var a = mesh.AddVertex(new Vector3(1f, 2f, 3f));
        var b = mesh.AddVertex(new Vector3(0f, 0f, 0f));

        VertexTransform.Translate(mesh, [a], new Vector3(10f, 0f, 0f));

        Assert.Equal(new Vector3(11f, 2f, 3f), mesh.Vertices[a]);
        Assert.Equal(Vector3.Zero, mesh.Vertices[b]); // não afetado
    }

    [Fact]
    public void Translate_ZeroDelta_DoesNotBumpVersion()
    {
        var mesh = new Mesh("Teste");
        var a = mesh.AddVertex(Vector3.One);
        var versionBefore = mesh.Version;

        VertexTransform.Translate(mesh, [a], Vector3.Zero);

        Assert.Equal(versionBefore, mesh.Version);
    }

    [Fact]
    public void Rotate_NinetyDegreesAroundZ_MatchesTheQuaternionOracle()
    {
        var mesh = new Mesh("Teste");
        var pivot = new Vector3(1f, 1f, 0f);
        var v = mesh.AddVertex(new Vector3(3f, 1f, 5f));
        var angle = MathF.PI / 2f;

        VertexTransform.Rotate(mesh, [v], pivot, Vector3.UnitZ, angle);

        var expected = pivot + Vector3.Transform(new Vector3(3f, 1f, 5f) - pivot, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, angle));
        Assert.True(Vector3.Distance(expected, mesh.Vertices[v]) < 1e-4f);
    }

    [Fact]
    public void Rotate_ZeroAngle_DoesNotBumpVersion()
    {
        var mesh = new Mesh("Teste");
        var v = mesh.AddVertex(Vector3.One);
        var versionBefore = mesh.Version;

        VertexTransform.Rotate(mesh, [v], Vector3.Zero, Vector3.UnitZ, 0f);

        Assert.Equal(versionBefore, mesh.Version);
    }

    [Fact]
    public void Scale_DoublesDistanceFromPivot()
    {
        var mesh = new Mesh("Teste");
        var pivot = new Vector3(1f, 0f, 0f);
        var v = mesh.AddVertex(new Vector3(3f, 0f, 0f)); // 2 unidades do pivô

        VertexTransform.Scale(mesh, [v], pivot, 2f);

        Assert.Equal(new Vector3(5f, 0f, 0f), mesh.Vertices[v]); // 4 unidades do pivô
    }

    [Fact]
    public void Scale_FactorOne_DoesNotBumpVersion()
    {
        var mesh = new Mesh("Teste");
        var v = mesh.AddVertex(Vector3.One);
        var versionBefore = mesh.Version;

        VertexTransform.Scale(mesh, [v], Vector3.Zero, 1f);

        Assert.Equal(versionBefore, mesh.Version);
    }

    [Fact]
    public void Mirror_ReflectsPositionsAndFlipsTriangleWinding()
    {
        var mesh = new Mesh("Teste");
        var a = mesh.AddVertex(new Vector3(1f, 0f, 0f));
        var b = mesh.AddVertex(new Vector3(2f, 1f, 0f));
        var c = mesh.AddVertex(new Vector3(2f, -1f, 0f));
        mesh.AddTriangle(a, b, c);

        VertexTransform.Mirror(mesh, new HashSet<int> { a, b, c }, Vector3.Zero, Vector3.UnitX);

        Assert.Equal(new Vector3(-1f, 0f, 0f), mesh.Vertices[a]);
        Assert.Equal(new Vector3(-2f, 1f, 0f), mesh.Vertices[b]);
        Assert.Equal(new Vector3(-2f, -1f, 0f), mesh.Vertices[c]);
        Assert.Equal((a, c, b), mesh.Triangles[0]); // giro invertido
    }

    [Fact]
    public void Mirror_EmptySet_DoesNothing()
    {
        var mesh = new Mesh("Teste");
        mesh.AddVertex(Vector3.One);
        var versionBefore = mesh.Version;

        VertexTransform.Mirror(mesh, new HashSet<int>(), Vector3.Zero, Vector3.UnitX);

        Assert.Equal(versionBefore, mesh.Version);
    }
}
