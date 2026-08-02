using System.Linq;
using System.Numerics;
using Structa.Core.Selection;
using Structa.Editor;
using Structa.Editor.Tools;
using Structa.Geometry;
using Structa.Selection;

namespace Structa.Editor.Tests;

public class RotateToolTests
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
    public void UpdateDrag_QuarterTurnAroundZ_RecoversApproximatelyNinetyDegrees()
    {
        var (scene, box) = CreateSceneWithBox();
        var tool = new RotateTool(scene);

        // Início na direção +X do pivô (origem, já que a caixa está centrada nela), giro até +Y —
        // um quarto de volta no plano Z=0 em torno de Z.
        tool.TryBegin(WholeObjectSelection(box), ScreenPointFor(new Vector3(1f, 0f, 0f)), ViewportSize, CameraPosition, View, Projection);
        tool.UpdateDrag(ScreenPointFor(new Vector3(0f, 1f, 0f)), ViewportSize, CameraPosition, View, Projection);

        var expectedDegrees = 90f;
        var actualDegrees = tool.CurrentAngleRadians * 180f / MathF.PI;
        Assert.True(MathF.Abs(expectedDegrees - actualDegrees) < 1f, $"esperado ~90°, obtido {actualDegrees}°");
    }

    [Fact]
    public void Commit_RotatesVerticesAroundThePivotMatchingTheQuaternionOracle()
    {
        var (scene, box) = CreateSceneWithBox();
        var originalVertices = box.Vertices.ToArray();
        var tool = new RotateTool(scene);
        tool.TryBegin(WholeObjectSelection(box), ScreenPointFor(new Vector3(1f, 0f, 0f)), ViewportSize, CameraPosition, View, Projection);
        tool.UpdateDrag(ScreenPointFor(new Vector3(0f, 1f, 0f)), ViewportSize, CameraPosition, View, Projection);

        var angle = tool.CurrentAngleRadians;
        var pivot = tool.Pivot;
        var rotated = tool.Commit();

        Assert.True(rotated);
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, angle);
        for (var i = 0; i < originalVertices.Length; i++)
        {
            var expected = pivot + Vector3.Transform(originalVertices[i] - pivot, rotation);
            Assert.True(Vector3.Distance(expected, box.Vertices[i]) < 1e-2f);
        }
    }

    [Fact]
    public void AppendAngleCharacter_OverridesTheDraggedAngleInDegrees()
    {
        var (scene, box) = CreateSceneWithBox();
        var tool = new RotateTool(scene);
        tool.TryBegin(WholeObjectSelection(box), ScreenPointFor(new Vector3(1f, 0f, 0f)), ViewportSize, CameraPosition, View, Projection);

        tool.AppendAngleCharacter('4');
        tool.AppendAngleCharacter('5');

        Assert.Equal("45", tool.TypedAngleText);
        Assert.True(MathF.Abs((tool.CurrentAngleRadians * 180f / MathF.PI) - 45f) < 1e-2f);
    }

    [Fact]
    public void Cancel_DiscardsWithoutMutatingTheMesh()
    {
        var (scene, box) = CreateSceneWithBox();
        var originalVertex0 = box.Vertices[0];
        var tool = new RotateTool(scene);
        tool.TryBegin(WholeObjectSelection(box), ScreenPointFor(new Vector3(1f, 0f, 0f)), ViewportSize, CameraPosition, View, Projection);
        tool.UpdateDrag(ScreenPointFor(new Vector3(0f, 1f, 0f)), ViewportSize, CameraPosition, View, Projection);

        tool.Cancel();

        Assert.False(tool.IsActive);
        Assert.Equal(originalVertex0, box.Vertices[0]);
    }
}
