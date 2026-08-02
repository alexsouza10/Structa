using System.Numerics;
using Structa.Geometry.Faces;

namespace Structa.Geometry.Tests;

public class FaceDetectorTests
{
    [Fact]
    public void TryDetectFace_ClosingATriangle_CreatesTheFace()
    {
        var mesh = new Mesh("Teste");
        var a = mesh.AddVertex(new Vector3(0f, 0f, 0f));
        var b = mesh.AddVertex(new Vector3(1f, 0f, 0f));
        var c = mesh.AddVertex(new Vector3(0f, 1f, 0f));
        mesh.AddEdge(a, b);
        mesh.AddEdge(b, c);
        mesh.AddEdge(c, a); // aresta que "fecha" o triângulo

        var created = FaceDetector.TryDetectFace(mesh, c, a);

        Assert.True(created);
        Assert.Single(mesh.Triangles);
        Assert.True(mesh.HasTriangle(a, b, c));
    }

    [Fact]
    public void TryDetectFace_WithoutAnAlternatePath_DoesNotCreateAFace()
    {
        var mesh = new Mesh("Teste");
        var a = mesh.AddVertex(Vector3.Zero);
        var b = mesh.AddVertex(Vector3.UnitX);
        mesh.AddEdge(a, b);

        var created = FaceDetector.TryDetectFace(mesh, a, b);

        Assert.False(created);
        Assert.Empty(mesh.Triangles);
    }

    [Fact]
    public void TryDetectFace_NonPlanarLoop_DoesNotCreateAFace()
    {
        var mesh = new Mesh("Teste");
        var a = mesh.AddVertex(new Vector3(0f, 0f, 0f));
        var b = mesh.AddVertex(new Vector3(1f, 0f, 0f));
        var c = mesh.AddVertex(new Vector3(1f, 1f, 0f));
        var d = mesh.AddVertex(new Vector3(0f, 1f, 5f)); // bem fora do plano dos outros três
        mesh.AddEdge(a, b);
        mesh.AddEdge(b, c);
        mesh.AddEdge(c, d);
        mesh.AddEdge(d, a);

        var created = FaceDetector.TryDetectFace(mesh, d, a);

        Assert.False(created);
        Assert.Empty(mesh.Triangles);
    }

    [Fact]
    public void TryDetectFace_ClosingAQuad_TriangulatesCoveringTheFullAreaWithConsistentWinding()
    {
        var mesh = new Mesh("Teste");
        var a = mesh.AddVertex(new Vector3(0f, 0f, 0f));
        var b = mesh.AddVertex(new Vector3(1f, 0f, 0f));
        var c = mesh.AddVertex(new Vector3(1f, 1f, 0f));
        var d = mesh.AddVertex(new Vector3(0f, 1f, 0f));
        mesh.AddEdge(a, b);
        mesh.AddEdge(b, c);
        mesh.AddEdge(c, d);
        mesh.AddEdge(d, a);

        var created = FaceDetector.TryDetectFace(mesh, d, a);

        Assert.True(created);
        Assert.Equal(2, mesh.Triangles.Count);

        // Para um loop plano, qual lado vira a frente é arbitrário (depende da direção de travessia
        // do BFS) — o mesmo ocorre no SketchUp, que deixa o usuário inverter a face se preciso. O que
        // precisa ser garantido é: (1) os dois triângulos concordam sobre qual lado é a frente (nenhum
        // "borboleta" com normais opostas) e (2) juntos cobrem exatamente a área do quadrado.
        Vector3? expectedDirection = null;
        var totalArea = 0f;

        foreach (var (t0, t1, t2) in mesh.Triangles)
        {
            var normal = Vector3.Cross(mesh.Vertices[t1] - mesh.Vertices[t0], mesh.Vertices[t2] - mesh.Vertices[t0]);

            expectedDirection ??= Vector3.Normalize(normal);
            Assert.True(Vector3.Dot(expectedDirection.Value, Vector3.Normalize(normal)) > 0.99f, "triângulos da mesma face com normais divergentes");

            totalArea += normal.Length() / 2f;
        }

        Assert.True(MathF.Abs(totalArea - 1f) < 1e-3f, $"área esperada 1, obtida {totalArea}");
    }

    [Fact]
    public void TryDetectFace_TriangleAlreadyCreated_DoesNotDuplicateTheFace()
    {
        var mesh = new Mesh("Teste");
        var a = mesh.AddVertex(new Vector3(0f, 0f, 0f));
        var b = mesh.AddVertex(new Vector3(1f, 0f, 0f));
        var c = mesh.AddVertex(new Vector3(0f, 1f, 0f));
        mesh.AddEdge(a, b);
        mesh.AddEdge(b, c);
        mesh.AddEdge(c, a);
        mesh.AddTriangle(a, b, c); // simula uma face já existente para esse mesmo loop

        var created = FaceDetector.TryDetectFace(mesh, c, a);

        Assert.False(created);
        Assert.Single(mesh.Triangles);
    }
}
