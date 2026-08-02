using System.Linq;
using System.Numerics;
using Structa.Core.Selection;
using Structa.Editor;
using Structa.Editor.Tools;
using Structa.Geometry;
using Structa.Selection;

namespace Structa.Editor.Tests;

public class ScaleToolTests
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

    // O ScaleTool mede distância no plano de frente para a câmera passando pelo pivô (normal =
    // pivô - câmera). Com pivô na origem e câmera em (6,6,6), esse plano é x+y+z=0 — os pontos de
    // clique usados nos testes de fator precisam estar exatamente nele para o oráculo (projetar um
    // ponto 3D conhecido e recuperá-lo de volta) valer; fora do plano, a interseção do raio recupera
    // outro ponto qualquer ao longo do raio, não o ponto originalmente projetado.
    private static readonly Vector3 OnPlaneNear = new(1f, -1f, 0f); // 1+(-1)+0 = 0, distância √2 da origem
    private static readonly Vector3 OnPlaneFar = new(2f, -2f, 0f); // mesma direção, distância 2√2 (fator 2)

    [Fact]
    public void UpdateDrag_TwiceAsFarFromPivot_RecoversFactorTwo()
    {
        var (scene, box) = CreateSceneWithBox();
        var tool = new ScaleTool(scene);
        tool.TryBegin(WholeObjectSelection(box), ScreenPointFor(OnPlaneNear), ViewportSize, CameraPosition, View, Projection);

        tool.UpdateDrag(ScreenPointFor(OnPlaneFar), ViewportSize, CameraPosition, View, Projection);

        Assert.True(MathF.Abs(tool.CurrentFactor - 2f) < 1e-2f, $"fator obtido {tool.CurrentFactor}");
    }

    [Fact]
    public void Commit_ScalesEveryVertexAroundThePivot()
    {
        var (scene, box) = CreateSceneWithBox(); // pivô = origem, vértices em ±1
        var originalVertices = box.Vertices.ToArray();
        var tool = new ScaleTool(scene);
        tool.TryBegin(WholeObjectSelection(box), ScreenPointFor(OnPlaneNear), ViewportSize, CameraPosition, View, Projection);
        tool.UpdateDrag(ScreenPointFor(OnPlaneFar), ViewportSize, CameraPosition, View, Projection);

        var factor = tool.CurrentFactor;
        var scaled = tool.Commit();

        Assert.True(scaled);
        for (var i = 0; i < originalVertices.Length; i++)
        {
            Assert.True(Vector3.Distance(originalVertices[i] * factor, box.Vertices[i]) < 1e-2f);
        }
    }

    [Fact]
    public void AppendFactorCharacter_OverridesTheDraggedFactor()
    {
        var (scene, box) = CreateSceneWithBox();
        var tool = new ScaleTool(scene);
        tool.TryBegin(WholeObjectSelection(box), ScreenPointFor(new Vector3(2f, 0f, 0f)), ViewportSize, CameraPosition, View, Projection);

        tool.AppendFactorCharacter('0');
        tool.AppendFactorCharacter('.');
        tool.AppendFactorCharacter('5');

        Assert.Equal("0.5", tool.TypedFactorText);
        Assert.True(MathF.Abs(tool.CurrentFactor - 0.5f) < 1e-3f);
    }

    [Fact]
    public void Cancel_DiscardsWithoutMutatingTheMesh()
    {
        var (scene, box) = CreateSceneWithBox();
        var originalVertex0 = box.Vertices[0];
        var tool = new ScaleTool(scene);
        tool.TryBegin(WholeObjectSelection(box), ScreenPointFor(new Vector3(2f, 0f, 0f)), ViewportSize, CameraPosition, View, Projection);
        tool.UpdateDrag(ScreenPointFor(new Vector3(4f, 0f, 0f)), ViewportSize, CameraPosition, View, Projection);

        tool.Cancel();

        Assert.False(tool.IsActive);
        Assert.Equal(originalVertex0, box.Vertices[0]);
    }
}
