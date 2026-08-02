using System.Linq;
using System.Numerics;
using Structa.Editor;
using Structa.Geometry.Transform;
using Structa.Selection;

namespace Structa.Editor.Tools;

/// <summary>
/// Ferramenta Escalar: agarra a seleção atual no clique, arrasta para longe do centroide dela para
/// aumentar (perto, para diminuir) — o fator é a razão entre a distância atual do cursor ao pivô e a
/// distância no clique inicial. Dá para digitar um fator exato a qualquer momento. Esc cancela.
///
/// Limitação desta etapa: só escala uniforme (mesmo fator nos 3 eixos) a partir do centroide da seleção
/// — sem os manípulos de canto/aresta do SketchUp para escala não-uniforme ou a partir de um canto.
/// </summary>
public sealed class ScaleTool
{
    private readonly Scene _scene;
    private readonly NumericEntryBuffer _factorEntry = new();

    private Dictionary<Guid, HashSet<int>>? _affectedVertices;
    private Vector3 _pivot;
    private Vector3 _planeNormal;
    private float _startDistance;
    private float _draggedFactor = 1f;

    public ScaleTool(Scene scene) => _scene = scene;

    public bool IsActive => _affectedVertices is not null;

    public Vector3 Pivot => _pivot;

    /// <summary>Fator efetivo atual: o valor digitado, se houver; senão, o fator arrastado. Nunca negativo.</summary>
    public float CurrentFactor => _factorEntry.TryGetValue(out var typed) ? MathF.Max(typed, 0f) : _draggedFactor;

    public string? TypedFactorText => _factorEntry.Text;

    public bool TryBegin(
        IReadOnlySet<SelectableElement> selection,
        Vector2 screenPoint, Vector2 viewportSize, Vector3 cameraPosition, Matrix4x4 view, Matrix4x4 projection)
    {
        var byMesh = SelectionVertexResolver.Resolve(selection, _scene.Meshes);
        if (byMesh.Count == 0)
        {
            return false;
        }

        var pivot = SelectionVertexResolver.ComputeCentroid(byMesh, _scene.Meshes);
        var planeNormal = Vector3.Normalize(pivot - cameraPosition);
        var ray = RayCaster.ScreenPointToRay(screenPoint, viewportSize, cameraPosition, view, projection);

        if (!RayIntersection.TryIntersectPlane(ray, pivot, planeNormal, out var startPoint))
        {
            return false;
        }

        var startDistance = Vector3.Distance(startPoint, pivot);
        if (startDistance < 1e-4f)
        {
            return false; // clicou muito perto do pivô: não dá para medir uma proporção de distância
        }

        _affectedVertices = byMesh;
        _pivot = pivot;
        _planeNormal = planeNormal;
        _startDistance = startDistance;
        _draggedFactor = 1f;
        _factorEntry.Clear();

        return true;
    }

    public void UpdateDrag(
        Vector2 screenPoint, Vector2 viewportSize, Vector3 cameraPosition, Matrix4x4 view, Matrix4x4 projection)
    {
        if (_affectedVertices is null)
        {
            return;
        }

        var ray = RayCaster.ScreenPointToRay(screenPoint, viewportSize, cameraPosition, view, projection);
        if (!RayIntersection.TryIntersectPlane(ray, _pivot, _planeNormal, out var currentPoint))
        {
            return;
        }

        _draggedFactor = Vector3.Distance(currentPoint, _pivot) / _startDistance;
    }

    public void AppendFactorCharacter(char character)
    {
        if (_affectedVertices is not null)
        {
            _factorEntry.Append(character);
        }
    }

    public void RemoveLastFactorCharacter() => _factorEntry.RemoveLast();

    public bool Commit()
    {
        if (_affectedVertices is null)
        {
            return false;
        }

        var factor = CurrentFactor;
        var scaled = MathF.Abs(factor - 1f) > 1e-4f;

        if (scaled)
        {
            foreach (var (meshId, vertices) in _affectedVertices)
            {
                VertexTransform.Scale(_scene.Meshes.First(m => m.Id == meshId), vertices, _pivot, factor);
            }
        }

        Reset();
        return scaled;
    }

    public void Cancel() => Reset();

    public IReadOnlyList<Vector3>? GetPreviewSegments()
    {
        if (_affectedVertices is null)
        {
            return null;
        }

        var factor = CurrentFactor;
        Vector3 Transform(Vector3 p) => _pivot + ((p - _pivot) * factor);

        var segments = new List<Vector3>();
        foreach (var (meshId, vertices) in _affectedVertices)
        {
            var mesh = _scene.Meshes.FirstOrDefault(m => m.Id == meshId);
            if (mesh is null)
            {
                continue;
            }

            segments.AddRange(GhostGeometry.BuildTransformedEdgeSegments(mesh, vertices, Transform));
        }

        return segments;
    }

    private void Reset()
    {
        _affectedVertices = null;
        _factorEntry.Clear();
        _draggedFactor = 1f;
    }
}
