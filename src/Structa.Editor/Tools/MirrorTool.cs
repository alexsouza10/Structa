using System.Linq;
using System.Numerics;
using Structa.Core.Editor;
using Structa.Editor;
using Structa.Geometry.Transform;
using Structa.Selection;

namespace Structa.Editor.Tools;

/// <summary>
/// Comando Espelhar: reflete a seleção atual, instantaneamente (sem arrasto), num plano perpendicular
/// ao eixo escolhido, passando pelo centroide combinado da seleção.
///
/// Limitação desta etapa: só espelha malhas selecionadas por inteiro (modo Objeto) — refletir um
/// subconjunto de vértices/arestas/faces poderia rasgar a malha (metade espelhada, metade não), então
/// esses casos são ignorados em vez de arriscar geometria incoerente.
/// </summary>
public sealed class MirrorTool
{
    private readonly Scene _scene;

    public MirrorTool(Scene scene) => _scene = scene;

    /// <summary>Retorna false sem alterar nada se a seleção estiver vazia ou nenhuma malha afetada
    /// estiver inteiramente selecionada.</summary>
    public bool Mirror(IReadOnlySet<SelectableElement> selection, MirrorAxis axis)
    {
        var byMesh = SelectionVertexResolver.Resolve(selection, _scene.Meshes);
        if (byMesh.Count == 0)
        {
            return false;
        }

        var wholeMeshes = byMesh.Where(pair => IsWholeMesh(pair.Key, pair.Value)).ToDictionary(pair => pair.Key, pair => pair.Value);
        if (wholeMeshes.Count == 0)
        {
            return false;
        }

        var pivot = SelectionVertexResolver.ComputeCentroid(wholeMeshes, _scene.Meshes);
        var normal = axis switch
        {
            MirrorAxis.X => Vector3.UnitX,
            MirrorAxis.Y => Vector3.UnitY,
            _ => Vector3.UnitZ,
        };

        foreach (var (meshId, vertices) in wholeMeshes)
        {
            VertexTransform.Mirror(_scene.Meshes.First(m => m.Id == meshId), vertices, pivot, normal);
        }

        return true;
    }

    private bool IsWholeMesh(Guid meshId, HashSet<int> vertices)
    {
        var mesh = _scene.Meshes.FirstOrDefault(m => m.Id == meshId);
        return mesh is not null && vertices.Count == mesh.Vertices.Count;
    }
}
