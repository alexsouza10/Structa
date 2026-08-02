using System.Numerics;
using Structa.Editor;
using Structa.Editor.Tools;
using Structa.Geometry;
using Structa.Selection;

namespace Structa.Editor.Tests;

/// <summary>
/// Testa a máquina de estados do <see cref="PushPullTool"/> (agarrar face, arrastar, digitar distância
/// exata, confirmar, cancelar) contra a caixa de teste (topo em Z=1). A câmera fica num ângulo (não
/// olhando reto para baixo pelo eixo Z, que é a normal do topo) para que arrastar ao longo da normal
/// corresponda a um movimento de tela não-degenerado.
/// </summary>
public class PushPullToolTests
{
    private static readonly Vector2 ViewportSize = new(800f, 600f);
    private static readonly Vector3 CameraPosition = new(6f, 6f, 6f);
    private static readonly Matrix4x4 View = Matrix4x4.CreateLookAt(CameraPosition, Vector3.Zero, Vector3.UnitZ);
    private static readonly Matrix4x4 Projection =
        Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, ViewportSize.X / ViewportSize.Y, 0.1f, 1000f);

    private static (Scene Scene, Mesh Box) CreateSceneWithBox()
    {
        var scene = new Scene();
        var box = MeshPrimitives.CreateBox("Caixa", Vector3.Zero, 2f); // topo em Z=1, base em Z=-1
        scene.Meshes.Add(box);
        return (scene, box);
    }

    private static Vector2 ScreenPointFor(Vector3 worldPoint)
    {
        Assert.True(RayCaster.TryWorldToScreen(worldPoint, ViewportSize, View, Projection, out var screen));
        return screen;
    }

    [Fact]
    public void TryBegin_ClickOnBoxTopFace_ActivatesAndCapturesTheFaceBoundary()
    {
        var (scene, _) = CreateSceneWithBox();
        var tool = new PushPullTool(scene);

        var began = tool.TryBegin(ScreenPointFor(new Vector3(0f, 0f, 1f)), ViewportSize, CameraPosition, View, Projection);

        Assert.True(began);
        Assert.True(tool.IsActive);

        var loop = tool.GetBoundaryLoopPositions();
        Assert.NotNull(loop);
        Assert.Equal(4, loop!.Count);
        Assert.All(loop, p => Assert.True(MathF.Abs(p.Z - 1f) < 1e-3f));
    }

    [Fact]
    public void TryBegin_MissingAllGeometry_ReturnsFalseAndStaysInactive()
    {
        var (scene, _) = CreateSceneWithBox();
        var tool = new PushPullTool(scene);

        // Canto da viewport: a caixa (só até +-1 em cada eixo) fica bem mais perto do centro da tela.
        var began = tool.TryBegin(new Vector2(10f, 10f), ViewportSize, CameraPosition, View, Projection);

        Assert.False(began);
        Assert.False(tool.IsActive);
    }

    [Fact]
    public void UpdateDrag_TowardsAKnownPointOnTheNormalAxis_RecoversTheExpectedDistance()
    {
        var (scene, _) = CreateSceneWithBox();
        var tool = new PushPullTool(scene);
        tool.TryBegin(ScreenPointFor(new Vector3(0f, 0f, 1f)), ViewportSize, CameraPosition, View, Projection);

        // (0,0,3) está exatamente sobre a reta que passa por (0,0,1) na direção da normal do topo (+Z),
        // 2 unidades adiante — arrastar até esse pixel deve recuperar distância 2.
        tool.UpdateDrag(ScreenPointFor(new Vector3(0f, 0f, 3f)), ViewportSize, CameraPosition, View, Projection);

        Assert.True(MathF.Abs(tool.CurrentDistance - 2f) < 1e-2f, $"esperado ~2, obtido {tool.CurrentDistance}");
    }

    [Fact]
    public void AppendDistanceCharacter_OverridesTheDraggedDistance()
    {
        var (scene, _) = CreateSceneWithBox();
        var tool = new PushPullTool(scene);
        tool.TryBegin(ScreenPointFor(new Vector3(0f, 0f, 1f)), ViewportSize, CameraPosition, View, Projection);
        tool.UpdateDrag(ScreenPointFor(new Vector3(0f, 0f, 3f)), ViewportSize, CameraPosition, View, Projection);

        tool.AppendDistanceCharacter('5');

        Assert.Equal("5", tool.TypedDistanceText);
        Assert.Equal(5f, tool.CurrentDistance, 3);

        tool.RemoveLastDistanceCharacter();

        Assert.Null(tool.TypedDistanceText);
        Assert.True(MathF.Abs(tool.CurrentDistance - 2f) < 1e-2f); // volta a valer o valor arrastado
    }

    [Fact]
    public void Commit_WithTypedDistance_ExtrudesTheFaceByExactlyThatAmount()
    {
        var (scene, box) = CreateSceneWithBox();
        var tool = new PushPullTool(scene);
        tool.TryBegin(ScreenPointFor(new Vector3(0f, 0f, 1f)), ViewportSize, CameraPosition, View, Projection);
        tool.AppendDistanceCharacter('3');

        var committed = tool.Commit();

        Assert.True(committed);
        Assert.False(tool.IsActive);
        Assert.Contains(box.Vertices, v => MathF.Abs(v.Z - 4f) < 1e-3f); // topo subiu de 1 para 1+3
    }

    [Fact]
    public void Commit_WithoutDraggingOrTyping_DoesNothing()
    {
        var (scene, box) = CreateSceneWithBox();
        var tool = new PushPullTool(scene);
        tool.TryBegin(ScreenPointFor(new Vector3(0f, 0f, 1f)), ViewportSize, CameraPosition, View, Projection);
        var versionBefore = box.Version;

        var committed = tool.Commit();

        Assert.False(committed);
        Assert.Equal(versionBefore, box.Version);
    }

    [Fact]
    public void Cancel_DiscardsTheOperationWithoutMutatingTheMesh()
    {
        var (scene, box) = CreateSceneWithBox();
        var tool = new PushPullTool(scene);
        tool.TryBegin(ScreenPointFor(new Vector3(0f, 0f, 1f)), ViewportSize, CameraPosition, View, Projection);
        tool.AppendDistanceCharacter('9');
        var versionBefore = box.Version;

        tool.Cancel();

        Assert.False(tool.IsActive);
        Assert.Null(tool.GetBoundaryLoopPositions());
        Assert.Equal(versionBefore, box.Version);
        Assert.DoesNotContain(box.Vertices, v => MathF.Abs(v.Z - 10f) < 1e-3f);
    }
}
