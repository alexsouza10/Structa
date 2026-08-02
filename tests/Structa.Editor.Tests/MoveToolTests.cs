using System.Linq;
using System.Numerics;
using Structa.Core.Selection;
using Structa.Editor;
using Structa.Editor.Tools;
using Structa.Geometry;
using Structa.Selection;

namespace Structa.Editor.Tests;

/// <summary>
/// Testa a máquina de estados do <see cref="MoveTool"/> (agarrar seleção, travar em eixo, digitar
/// distância, duplicar, cancelar) contra a caixa de teste, com a mesma câmera angulada usada em
/// <c>PushPullToolTests</c> — precisa não estar olhando reto por um eixo para o arrasto ser bem definido.
/// </summary>
public class MoveToolTests
{
    private static readonly Vector2 ViewportSize = new(800f, 600f);
    private static readonly Vector3 CameraPosition = new(6f, 6f, 6f);
    private static readonly Matrix4x4 View = Matrix4x4.CreateLookAt(CameraPosition, Vector3.Zero, Vector3.UnitZ);
    private static readonly Matrix4x4 Projection =
        Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, ViewportSize.X / ViewportSize.Y, 0.1f, 1000f);

    private static (Scene Scene, Mesh Box) CreateSceneWithBox()
    {
        var scene = new Scene();
        var box = MeshPrimitives.CreateBox("Caixa", Vector3.Zero, 2f); // centrada na origem
        scene.Meshes.Add(box);
        return (scene, box);
    }

    private static Vector2 ScreenPointFor(Vector3 worldPoint)
    {
        Assert.True(RayCaster.TryWorldToScreen(worldPoint, ViewportSize, View, Projection, out var screen));
        return screen;
    }

    private static HashSet<SelectableElement> WholeObjectSelection(Mesh mesh) =>
        [new SelectableElement(mesh.Id, SelectionMode.Object, 0)];

    [Fact]
    public void TryBegin_EmptySelection_ReturnsFalse()
    {
        var (scene, _) = CreateSceneWithBox();
        var tool = new MoveTool(scene);

        var began = tool.TryBegin(new HashSet<SelectableElement>(), false, ScreenPointFor(Vector3.Zero), ViewportSize, CameraPosition, View, Projection);

        Assert.False(began);
        Assert.False(tool.IsActive);
    }

    [Fact]
    public void UpdateDrag_TowardsAKnownPointOnAnAxis_LocksToThatAxisAndRecoversTheDelta()
    {
        var (scene, box) = CreateSceneWithBox();
        var tool = new MoveTool(scene);
        tool.TryBegin(WholeObjectSelection(box), false, ScreenPointFor(Vector3.Zero), ViewportSize, CameraPosition, View, Projection);

        tool.UpdateDrag(ScreenPointFor(new Vector3(3f, 0f, 0f)), ViewportSize, CameraPosition, View, Projection);

        Assert.Equal(0, tool.LockedAxisIndex); // eixo X
        Assert.True(Vector3.Distance(new Vector3(3f, 0f, 0f), tool.CurrentDelta) < 1e-2f, $"delta obtido {tool.CurrentDelta}");
    }

    [Fact]
    public void Commit_TranslatesEveryAffectedVertexByTheDelta()
    {
        var (scene, box) = CreateSceneWithBox();
        var originalVertices = box.Vertices.ToArray();
        var tool = new MoveTool(scene);
        tool.TryBegin(WholeObjectSelection(box), false, ScreenPointFor(Vector3.Zero), ViewportSize, CameraPosition, View, Projection);
        tool.UpdateDrag(ScreenPointFor(new Vector3(3f, 0f, 0f)), ViewportSize, CameraPosition, View, Projection);

        var moved = tool.Commit();

        Assert.True(moved);
        Assert.False(tool.IsActive);
        for (var i = 0; i < originalVertices.Length; i++)
        {
            Assert.True(Vector3.Distance(originalVertices[i] + new Vector3(3f, 0f, 0f), box.Vertices[i]) < 1e-2f);
        }
    }

    [Fact]
    public void AppendDistanceCharacter_OverridesTheDraggedDistanceAlongTheSameDirection()
    {
        var (scene, box) = CreateSceneWithBox();
        var tool = new MoveTool(scene);
        tool.TryBegin(WholeObjectSelection(box), false, ScreenPointFor(Vector3.Zero), ViewportSize, CameraPosition, View, Projection);
        tool.UpdateDrag(ScreenPointFor(new Vector3(3f, 0f, 0f)), ViewportSize, CameraPosition, View, Projection); // define a direção +X

        tool.AppendDistanceCharacter('5');

        Assert.Equal("5", tool.TypedDistanceText);
        Assert.True(Vector3.Distance(new Vector3(5f, 0f, 0f), tool.CurrentDelta) < 1e-2f, $"delta obtido {tool.CurrentDelta}");
    }

    [Fact]
    public void Cancel_DiscardsWithoutMutatingTheMesh()
    {
        var (scene, box) = CreateSceneWithBox();
        var originalVertex0 = box.Vertices[0];
        var tool = new MoveTool(scene);
        tool.TryBegin(WholeObjectSelection(box), false, ScreenPointFor(Vector3.Zero), ViewportSize, CameraPosition, View, Projection);
        tool.UpdateDrag(ScreenPointFor(new Vector3(3f, 0f, 0f)), ViewportSize, CameraPosition, View, Projection);

        tool.Cancel();

        Assert.False(tool.IsActive);
        Assert.Equal(originalVertex0, box.Vertices[0]);
    }

    [Fact]
    public void TryBegin_WithDuplicateOnWholeObjectSelection_ClonesTheMeshAndLeavesTheOriginalInPlace()
    {
        var (scene, box) = CreateSceneWithBox();
        var originalVertex0 = box.Vertices[0];
        var tool = new MoveTool(scene);

        tool.TryBegin(WholeObjectSelection(box), true, ScreenPointFor(Vector3.Zero), ViewportSize, CameraPosition, View, Projection);
        tool.UpdateDrag(ScreenPointFor(new Vector3(3f, 0f, 0f)), ViewportSize, CameraPosition, View, Projection);
        var moved = tool.Commit();

        Assert.True(moved);
        Assert.Equal(2, scene.Meshes.Count); // original + cópia
        Assert.Equal(originalVertex0, box.Vertices[0]); // original intocado

        var clone = scene.Meshes.Single(m => m.Id != box.Id);
        Assert.True(Vector3.Distance(originalVertex0 + new Vector3(3f, 0f, 0f), clone.Vertices[0]) < 1e-2f);
    }

    [Fact]
    public void TryBegin_WithDuplicateOnPartialSelection_MovesInPlaceWithoutCloning()
    {
        var (scene, box) = CreateSceneWithBox();
        HashSet<SelectableElement> partialSelection = [new SelectableElement(box.Id, SelectionMode.Vertex, 0)];
        var tool = new MoveTool(scene);

        tool.TryBegin(partialSelection, true, ScreenPointFor(box.Vertices[0]), ViewportSize, CameraPosition, View, Projection);

        Assert.Single(scene.Meshes); // nenhuma cópia foi criada
    }
}
