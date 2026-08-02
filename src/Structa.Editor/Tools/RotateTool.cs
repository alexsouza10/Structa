using System.Linq;
using System.Numerics;
using Structa.Editor;
using Structa.Geometry.Transform;
using Structa.Selection;

namespace Structa.Editor.Tools;

/// <summary>
/// Ferramenta Rotacionar: agarra a seleção atual no clique, arrasta em torno do centroide dela para
/// girar (o ângulo é o quanto o cursor girou ao redor do pivô desde o clique inicial, como um
/// transferidor), solta para confirmar. Dá para digitar um ângulo exato em graus a qualquer momento.
/// Esc cancela sem alterar nada.
///
/// Limitação desta etapa: o eixo de rotação é sempre <see cref="Vector3.UnitZ"/> (vertical do mundo) —
/// não há escolha de plano/eixo pelo usuário, diferente do SketchUp real, que infere o plano a partir
/// de onde você passa o mouse. Cobre o caso mais comum (girar em planta) sem a complexidade de um
/// transferidor 3D completo.
/// </summary>
public sealed class RotateTool
{
    private static readonly Vector3 Axis = Vector3.UnitZ;

    private readonly Scene _scene;
    private readonly NumericEntryBuffer _angleEntry = new(); // graus, mais natural para digitar

    private Dictionary<Guid, HashSet<int>>? _affectedVertices;
    private Vector3 _pivot;
    private Vector3 _startDirection;
    private float _draggedAngleRadians;

    public RotateTool(Scene scene) => _scene = scene;

    public bool IsActive => _affectedVertices is not null;

    public Vector3 Pivot => _pivot;

    /// <summary>Ângulo efetivo atual em radianos: o valor digitado (em graus, convertido), se houver;
    /// senão, o ângulo arrastado.</summary>
    public float CurrentAngleRadians => _angleEntry.TryGetValue(out var degrees) ? degrees * MathF.PI / 180f : _draggedAngleRadians;

    public string? TypedAngleText => _angleEntry.Text;

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
        var ray = RayCaster.ScreenPointToRay(screenPoint, viewportSize, cameraPosition, view, projection);

        if (!RayIntersection.TryIntersectPlane(ray, pivot, Axis, out var startPoint))
        {
            return false;
        }

        var offset = startPoint - pivot;
        if (offset.LengthSquared() < 1e-6f)
        {
            return false; // clicou em cima do pivô: não há direção inicial para medir o ângulo
        }

        _affectedVertices = byMesh;
        _pivot = pivot;
        _startDirection = Vector3.Normalize(offset);
        _draggedAngleRadians = 0f;
        _angleEntry.Clear();

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
        if (!RayIntersection.TryIntersectPlane(ray, _pivot, Axis, out var currentPoint))
        {
            return;
        }

        var offset = currentPoint - _pivot;
        if (offset.LengthSquared() < 1e-6f)
        {
            return;
        }

        var currentDirection = Vector3.Normalize(offset);
        var dot = Math.Clamp(Vector3.Dot(_startDirection, currentDirection), -1f, 1f);
        var angle = MathF.Acos(dot);

        if (Vector3.Dot(Vector3.Cross(_startDirection, currentDirection), Axis) < 0f)
        {
            angle = -angle;
        }

        _draggedAngleRadians = angle;
    }

    public void AppendAngleCharacter(char character)
    {
        if (_affectedVertices is not null)
        {
            _angleEntry.Append(character);
        }
    }

    public void RemoveLastAngleCharacter() => _angleEntry.RemoveLast();

    public bool Commit()
    {
        if (_affectedVertices is null)
        {
            return false;
        }

        var angle = CurrentAngleRadians;
        var rotated = MathF.Abs(angle) > 1e-4f;

        if (rotated)
        {
            foreach (var (meshId, vertices) in _affectedVertices)
            {
                VertexTransform.Rotate(_scene.Meshes.First(m => m.Id == meshId), vertices, _pivot, Axis, angle);
            }
        }

        Reset();
        return rotated;
    }

    public void Cancel() => Reset();

    public IReadOnlyList<Vector3>? GetPreviewSegments()
    {
        if (_affectedVertices is null)
        {
            return null;
        }

        var rotation = Quaternion.CreateFromAxisAngle(Axis, CurrentAngleRadians);
        Vector3 Transform(Vector3 p) => _pivot + Vector3.Transform(p - _pivot, rotation);

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
        _angleEntry.Clear();
        _draggedAngleRadians = 0f;
    }
}
