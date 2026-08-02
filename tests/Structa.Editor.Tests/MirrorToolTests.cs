using System.Linq;
using System.Numerics;
using Structa.Core.Editor;
using Structa.Core.Selection;
using Structa.Editor;
using Structa.Editor.Tools;
using Structa.Geometry;
using Structa.Selection;

namespace Structa.Editor.Tests;

public class MirrorToolTests
{
    private static (Scene Scene, Mesh Box) CreateSceneWithBox()
    {
        var scene = new Scene();
        var box = MeshPrimitives.CreateBox("Caixa", Vector3.Zero, 2f); // centrada na origem, vértices em ±1
        scene.Meshes.Add(box);
        return (scene, box);
    }

    [Fact]
    public void Mirror_WholeObjectSelectionAcrossX_NegatesXAndFlipsTriangleWinding()
    {
        var (scene, box) = CreateSceneWithBox();
        var originalVertices = box.Vertices.ToArray();
        var originalTriangles = box.Triangles.ToArray();
        HashSet<SelectableElement> selection = [new SelectableElement(box.Id, SelectionMode.Object, 0)];
        var tool = new MirrorTool(scene);

        var mirrored = tool.Mirror(selection, MirrorAxis.X);

        Assert.True(mirrored);
        for (var i = 0; i < originalVertices.Length; i++)
        {
            var expected = originalVertices[i] with { X = -originalVertices[i].X };
            Assert.True(Vector3.Distance(expected, box.Vertices[i]) < 1e-4f);
        }

        for (var i = 0; i < originalTriangles.Length; i++)
        {
            var (a, b, c) = originalTriangles[i];
            Assert.Equal((a, c, b), box.Triangles[i]);
        }
    }

    [Fact]
    public void Mirror_PartialSelection_DoesNothing()
    {
        var (scene, box) = CreateSceneWithBox();
        var originalVertex0 = box.Vertices[0];
        HashSet<SelectableElement> partialSelection = [new SelectableElement(box.Id, SelectionMode.Face, 0)];
        var tool = new MirrorTool(scene);

        var mirrored = tool.Mirror(partialSelection, MirrorAxis.X);

        Assert.False(mirrored);
        Assert.Equal(originalVertex0, box.Vertices[0]);
    }

    [Fact]
    public void Mirror_EmptySelection_ReturnsFalse()
    {
        var (scene, _) = CreateSceneWithBox();
        var tool = new MirrorTool(scene);

        var mirrored = tool.Mirror(new HashSet<SelectableElement>(), MirrorAxis.Z);

        Assert.False(mirrored);
    }
}
