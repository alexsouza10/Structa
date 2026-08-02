using System.Linq;
using System.Numerics;
using Structa.Editor;
using Structa.Editor.Tools;
using Structa.Geometry;
using Structa.Selection;

namespace Structa.Editor.Tests;

/// <summary>
/// Testa a máquina de estados do <see cref="LineTool"/> (encadeamento, solda de vértices, fechamento
/// de loop e cancelamento) usando uma câmera fixa, olhando de cima para baixo. Os pontos de tela usados
/// nos testes são resolvidos por projeção livre no plano Z=0 — os pontos-mundo esperados são calculados
/// com os mesmos utilitários (<see cref="RayCaster"/>/<see cref="RayIntersection"/>) que o LineTool usa
/// internamente, já cobertos por <c>RayIntersectionTests</c>.
/// </summary>
public class LineToolTests
{
    private static readonly Vector2 ViewportSize = new(800f, 600f);
    private static readonly Vector3 CameraPosition = new(0f, 0f, 10f);
    private static readonly Matrix4x4 View = Matrix4x4.CreateLookAt(CameraPosition, Vector3.Zero, Vector3.UnitY);
    private static readonly Matrix4x4 Projection =
        Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, ViewportSize.X / ViewportSize.Y, 0.1f, 1000f);

    // Longe o bastante do centro (e um do outro) para não cair na tolerância de inferência de eixo.
    private static readonly Vector2 Point1Screen = new(400f, 300f);
    private static readonly Vector2 Point2Screen = new(520f, 360f);
    private static readonly Vector2 Point3Screen = new(300f, 420f);

    [Fact]
    public void Click_Once_OnlySetsStartPoint()
    {
        var tool = new LineTool(new Scene());

        tool.Click(Point1Screen, ViewportSize, CameraPosition, View, Projection);

        Assert.True(tool.IsDrawing);
        AssertClose(GroundPointFor(Point1Screen), tool.StartPoint!.Value);
    }

    [Fact]
    public void Click_Twice_CreatesEdgeInNewSketchMeshAndChainsNextSegment()
    {
        var scene = new Scene();
        var tool = new LineTool(scene);

        tool.Click(Point1Screen, ViewportSize, CameraPosition, View, Projection);
        tool.Click(Point2Screen, ViewportSize, CameraPosition, View, Projection);

        var mesh = Assert.Single(scene.Meshes);
        Assert.Equal(2, mesh.Vertices.Count);
        Assert.True(mesh.HasEdge(0, 1));
        Assert.True(tool.IsDrawing);
        AssertClose(GroundPointFor(Point2Screen), tool.StartPoint!.Value);
    }

    [Fact]
    public void Click_BackAtFirstPoint_ClosesTheLoopByWeldingToTheExistingSketchVertexAndCreatesAFace()
    {
        var scene = new Scene();
        var tool = new LineTool(scene);

        tool.Click(Point1Screen, ViewportSize, CameraPosition, View, Projection); // início do triângulo
        tool.Click(Point2Screen, ViewportSize, CameraPosition, View, Projection); // aresta 0-1
        tool.Click(Point3Screen, ViewportSize, CameraPosition, View, Projection); // aresta 1-2
        tool.Click(Point1Screen, ViewportSize, CameraPosition, View, Projection); // fecha: aresta 2-0

        var mesh = Assert.Single(scene.Meshes);
        Assert.Equal(3, mesh.Vertices.Count); // fechou o loop sem criar um 4º vértice coincidente
        Assert.Equal(3, mesh.Edges.Count);
        Assert.True(mesh.HasEdge(0, 1));
        Assert.True(mesh.HasEdge(1, 2));
        Assert.True(mesh.HasEdge(2, 0));

        // Todos os cliques caem no plano Z=0 (snap livre), então o loop é plano — a aresta que fecha
        // o triângulo deve disparar o FaceDetector automaticamente, como no SketchUp.
        Assert.Single(mesh.Triangles);
        Assert.True(mesh.HasTriangle(0, 1, 2));
    }

    [Fact]
    public void Click_NonPlanarLoop_ClosesEdgesButDoesNotCreateAFace()
    {
        // 3 pontos são sempre coplanares por definição — precisa de um quadrilátero (4 pontos) para o
        // teste de planaridade ser significativo: os 3 primeiros ficam no chão (Z=0), o 4º é elevado.
        var scene = new Scene();
        var reference = new Mesh("Referência");
        var elevated = reference.AddVertex(new Vector3(0.2f, 0.7f, 5f));
        scene.Meshes.Add(reference);

        Assert.True(RayCaster.TryWorldToScreen(reference.Vertices[elevated], ViewportSize, View, Projection, out var elevatedScreen));

        var tool = new LineTool(scene);
        tool.Click(Point1Screen, ViewportSize, CameraPosition, View, Projection);
        tool.Click(Point2Screen, ViewportSize, CameraPosition, View, Projection);
        tool.Click(Point3Screen, ViewportSize, CameraPosition, View, Projection);
        tool.Click(elevatedScreen, ViewportSize, CameraPosition, View, Projection);
        tool.Click(Point1Screen, ViewportSize, CameraPosition, View, Projection); // fecha o loop não-plano

        var sketch = scene.Meshes.Single(m => m.Id != reference.Id);
        Assert.Equal(4, sketch.Edges.Count); // as arestas foram criadas normalmente...
        Assert.Empty(sketch.Triangles); // ...mas nenhuma face, por não serem coplanares
    }

    [Fact]
    public void Click_SamePointTwice_DoesNotCreateZeroLengthEdgeOrDuplicateVertex()
    {
        var scene = new Scene();
        var tool = new LineTool(scene);

        tool.Click(Point1Screen, ViewportSize, CameraPosition, View, Projection);
        tool.Click(Point1Screen, ViewportSize, CameraPosition, View, Projection);

        var mesh = Assert.Single(scene.Meshes);
        Assert.Single(mesh.Vertices);
        Assert.Empty(mesh.Edges);
        Assert.True(tool.IsDrawing); // continua encadeando a partir do mesmo ponto
    }

    [Fact]
    public void Click_SnapsToVertexOfAnotherMesh_CopiesPositionWithoutMutatingIt()
    {
        var scene = new Scene();
        var reference = new Mesh("Referência");
        reference.AddVertex(new Vector3(1f, 1f, 0f));
        scene.Meshes.Add(reference);

        Assert.True(RayCaster.TryWorldToScreen(reference.Vertices[0], ViewportSize, View, Projection, out var vertexScreen));

        var tool = new LineTool(scene);
        tool.Click(vertexScreen, ViewportSize, CameraPosition, View, Projection);
        tool.Click(Point2Screen, ViewportSize, CameraPosition, View, Projection);

        Assert.Equal(2, scene.Meshes.Count);
        Assert.Single(reference.Vertices); // a malha de referência não foi alterada

        var sketch = scene.Meshes.Single(m => m.Id != reference.Id);
        Assert.Equal(2, sketch.Vertices.Count);
        AssertClose(reference.Vertices[0], sketch.Vertices[0]);
    }

    [Fact]
    public void Cancel_DiscardsPendingSegmentWithoutTouchingCommittedGeometry()
    {
        var scene = new Scene();
        var tool = new LineTool(scene);

        tool.Click(Point1Screen, ViewportSize, CameraPosition, View, Projection);
        tool.Click(Point2Screen, ViewportSize, CameraPosition, View, Projection);
        tool.Cancel();

        Assert.False(tool.IsDrawing);
        Assert.Null(tool.StartPoint);

        var mesh = Assert.Single(scene.Meshes);
        Assert.Equal(2, mesh.Vertices.Count);
        Assert.Single(mesh.Edges);
    }

    [Fact]
    public void Preview_DoesNotMutateSceneOrToolState()
    {
        var scene = new Scene();
        var tool = new LineTool(scene);

        tool.Preview(Point1Screen, ViewportSize, CameraPosition, View, Projection);
        tool.Preview(Point2Screen, ViewportSize, CameraPosition, View, Projection);

        Assert.False(tool.IsDrawing);
        Assert.Empty(scene.Meshes);
    }

    private static Vector3 GroundPointFor(Vector2 screenPoint)
    {
        var ray = RayCaster.ScreenPointToRay(screenPoint, ViewportSize, CameraPosition, View, Projection);
        Assert.True(RayIntersection.TryIntersectPlane(ray, Vector3.Zero, Vector3.UnitZ, out var point));
        return point;
    }

    private static void AssertClose(Vector3 expected, Vector3 actual, float tolerance = 1e-3f) =>
        Assert.True(Vector3.Distance(expected, actual) < tolerance, $"esperado {expected}, obtido {actual}");
}
