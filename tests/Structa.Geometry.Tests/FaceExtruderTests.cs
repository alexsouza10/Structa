using System.Numerics;
using Structa.Geometry.Faces;

namespace Structa.Geometry.Tests;

public class FaceExtruderTests
{
    [Fact]
    public void Extrude_BoxTopFace_MovesSharedVerticesInPlaceWithoutAddingGeometry()
    {
        var box = MeshPrimitives.CreateBox("Caixa", Vector3.Zero, 2f); // topo em Z=1, base em Z=-1
        var group = FaceGroupFinder.FindConnectedCoplanarTriangles(box, 2);
        var normal = FaceGroupFinder.TriangleNormal(box, 2);

        Assert.Equal(8, box.Vertices.Count);
        AssertVolume(box, 8f); // 2*2*2

        var extruded = FaceExtruder.Extrude(box, group, normal, 3f);

        Assert.True(extruded);
        Assert.Equal(8, box.Vertices.Count); // nada novo: topo já era compartilhado com as laterais
        Assert.Equal(12, box.Triangles.Count);

        foreach (var vertex in box.Vertices)
        {
            // todo vértice do topo (Z originalmente 1) subiu; os da base (Z=-1) ficaram parados
            Assert.True(MathF.Abs(vertex.Z - 4f) < 1e-4f || MathF.Abs(vertex.Z + 1f) < 1e-4f);
        }

        AssertVolume(box, 4f * (2f + 3f)); // base 2x2, altura nova 2+3
    }

    [Fact]
    public void Extrude_LoneFace_CreatesAClosedSolidWithNewCapAndWalls()
    {
        var mesh = new Mesh("Solta");
        var a = mesh.AddVertex(new Vector3(0f, 0f, 0f));
        var b = mesh.AddVertex(new Vector3(1f, 0f, 0f));
        var c = mesh.AddVertex(new Vector3(1f, 1f, 0f));
        var d = mesh.AddVertex(new Vector3(0f, 1f, 0f));
        mesh.AddEdge(a, b);
        mesh.AddEdge(b, c);
        mesh.AddEdge(c, d);
        mesh.AddEdge(d, a);
        mesh.AddTriangle(a, b, c); // normal +Z
        mesh.AddTriangle(a, c, d); // normal +Z

        var group = FaceGroupFinder.FindConnectedCoplanarTriangles(mesh, 0);
        Assert.Equal(2, group.Count);

        var extruded = FaceExtruder.Extrude(mesh, group, Vector3.UnitZ, 2f);

        Assert.True(extruded);
        Assert.Equal(8, mesh.Vertices.Count); // 4 originais (base) + 4 novos (topo)
        Assert.Equal(12, mesh.Triangles.Count); // 2 tampa base + 2 tampa topo + 4 paredes * 2

        AssertVolume(mesh, 1f * 1f * 2f); // base 1x1, altura 2
    }

    [Fact]
    public void Extrude_ZeroDistance_DoesNothing()
    {
        var box = MeshPrimitives.CreateBox("Caixa", Vector3.Zero, 2f);
        var group = FaceGroupFinder.FindConnectedCoplanarTriangles(box, 2);
        var versionBefore = box.Version;

        var extruded = FaceExtruder.Extrude(box, group, Vector3.UnitZ, 0f);

        Assert.False(extruded);
        Assert.Equal(versionBefore, box.Version);
    }

    [Fact]
    public void Extrude_NegativeDistance_PushesInwardWithCorrectVolume()
    {
        var box = MeshPrimitives.CreateBox("Caixa", Vector3.Zero, 2f);
        var group = FaceGroupFinder.FindConnectedCoplanarTriangles(box, 2);
        var normal = FaceGroupFinder.TriangleNormal(box, 2);

        var extruded = FaceExtruder.Extrude(box, group, normal, -0.5f);

        Assert.True(extruded);
        AssertVolume(box, 4f * (2f - 0.5f));
    }

    /// <summary>Volume de uma malha fechada e com winding para fora consistente, via o teorema da
    /// divergência (soma de tetraedros com sinal a partir da origem) — positivo confirma que a
    /// extrusão manteve o giro correto em todos os triângulos, não só a geometria certa.</summary>
    private static void AssertVolume(Mesh mesh, float expected, float tolerance = 1e-2f)
    {
        var volume = 0f;
        foreach (var (ia, ib, ic) in mesh.Triangles)
        {
            var a = mesh.Vertices[ia];
            var b = mesh.Vertices[ib];
            var c = mesh.Vertices[ic];
            volume += Vector3.Dot(a, Vector3.Cross(b, c)) / 6f;
        }

        Assert.True(MathF.Abs(volume - expected) < tolerance, $"volume esperado {expected}, obtido {volume}");
    }
}
