using System.Linq;
using System.Numerics;
using Structa.Editor;
using Structa.Geometry;
using Structa.Geometry.Transform;
using Structa.Selection;

namespace Structa.Editor.Tools;

/// <summary>
/// Ferramenta Mover: agarra a seleção atual (qualquer granularidade — vértices, arestas, faces ou
/// objetos) no clique, arrasta livremente num plano de frente para a câmera passando pelo centroide da
/// seleção, solta para confirmar. Perto de um dos eixos X/Y/Z partindo do ponto de clique, o movimento
/// trava nesse eixo (mesma inferência da ferramenta Linha). Como no Empurrar/Puxar, dá para digitar uma
/// distância exata a qualquer momento (na direção atual — travada ou livre) e Enter/soltar confirmam;
/// Esc cancela sem alterar nada.
///
/// Segurar Ctrl ao clicar duplica a seleção primeiro (só quando ela cobre malhas inteiras — modo Objeto;
/// ver <see cref="TryBegin"/>) e move a cópia, deixando o original no lugar — o "Duplicar" da Etapa 08
/// é este modificador, não uma ferramenta separada, do mesmo jeito que o SketchUp faz.
/// </summary>
public sealed class MoveTool
{
    private const float AxisPixelTolerance = 10f;

    private readonly Scene _scene;
    private readonly NumericEntryBuffer _distanceEntry = new();

    private Dictionary<Guid, HashSet<int>>? _affectedVertices;
    private Vector3 _pivot;
    private Vector3 _dragOrigin;
    private Vector3 _planeNormal;
    private Vector3 _currentDelta;
    private Vector3 _lastDirection = Vector3.UnitX;

    public MoveTool(Scene scene) => _scene = scene;

    /// <summary>Verdadeiro entre um <see cref="TryBegin"/> bem-sucedido e o commit/cancelamento.</summary>
    public bool IsActive => _affectedVertices is not null;

    /// <summary>Índice do eixo travado por inferência (0=X, 1=Y, 2=Z), ou nulo em arrasto livre.</summary>
    public int? LockedAxisIndex { get; private set; }

    /// <summary>Deslocamento efetivo atual: a distância digitada aplicada na última direção conhecida,
    /// se houver texto digitado; senão, o deslocamento arrastado (travado ou livre).</summary>
    public Vector3 CurrentDelta => _distanceEntry.TryGetValue(out var typed) ? _lastDirection * typed : _currentDelta;

    public string? TypedDistanceText => _distanceEntry.Text;

    /// <summary>
    /// Agarra a seleção informada. Retorna false se ela estiver vazia ou o clique não cair em nenhum
    /// plano válido. <paramref name="duplicate"/> (Ctrl) só duplica de fato quando cada malha afetada
    /// está selecionada por inteiro (modo Objeto) — seleção parcial (vértice/aresta/face) move normal,
    /// sem duplicar, para não arriscar extrair um pedaço incoerente de uma malha maior.
    /// </summary>
    public bool TryBegin(
        IReadOnlySet<SelectableElement> selection, bool duplicate,
        Vector2 screenPoint, Vector2 viewportSize, Vector3 cameraPosition, Matrix4x4 view, Matrix4x4 projection)
    {
        var byMesh = SelectionVertexResolver.Resolve(selection, _scene.Meshes);
        if (byMesh.Count == 0)
        {
            return false;
        }

        if (duplicate && CanDuplicate(byMesh))
        {
            byMesh = DuplicateMeshes(byMesh);
        }

        var pivot = SelectionVertexResolver.ComputeCentroid(byMesh, _scene.Meshes);
        var planeNormal = Vector3.Normalize(pivot - cameraPosition);
        var ray = RayCaster.ScreenPointToRay(screenPoint, viewportSize, cameraPosition, view, projection);

        if (!RayIntersection.TryIntersectPlane(ray, pivot, planeNormal, out var origin))
        {
            return false;
        }

        _affectedVertices = byMesh;
        _pivot = pivot;
        _planeNormal = planeNormal;
        _dragOrigin = origin;
        _currentDelta = Vector3.Zero;
        LockedAxisIndex = null;
        _distanceEntry.Clear();

        return true;
    }

    /// <summary>Atualiza o deslocamento arrastado: trava num eixo se o cursor estiver perto o bastante da
    /// sua projeção em tela a partir do ponto de clique original; senão, segue livre no plano de frente
    /// para a câmera.</summary>
    public void UpdateDrag(
        Vector2 screenPoint, Vector2 viewportSize, Vector3 cameraPosition, Matrix4x4 view, Matrix4x4 projection)
    {
        if (_affectedVertices is null)
        {
            return;
        }

        var ray = RayCaster.ScreenPointToRay(screenPoint, viewportSize, cameraPosition, view, projection);

        if (AxisSnapper.TryFindClosestAxis(
                ray, _dragOrigin, screenPoint, viewportSize, view, projection, AxisPixelTolerance,
                out var axisPoint, out var axisIndex))
        {
            LockedAxisIndex = axisIndex;
            _currentDelta = axisPoint - _dragOrigin;
        }
        else
        {
            LockedAxisIndex = null;
            if (RayIntersection.TryIntersectPlane(ray, _pivot, _planeNormal, out var freePoint))
            {
                _currentDelta = freePoint - _dragOrigin;
            }
        }

        if (_currentDelta.LengthSquared() > 1e-8f)
        {
            _lastDirection = Vector3.Normalize(_currentDelta);
        }
    }

    public void AppendDistanceCharacter(char character)
    {
        if (_affectedVertices is not null)
        {
            _distanceEntry.Append(character);
        }
    }

    public void RemoveLastDistanceCharacter() => _distanceEntry.RemoveLast();

    /// <summary>Confirma o deslocamento efetivo atual. Sem efeito (retorna false) se for ~zero ou não
    /// houver operação em andamento.</summary>
    public bool Commit()
    {
        if (_affectedVertices is null)
        {
            return false;
        }

        var delta = CurrentDelta;
        var moved = delta.LengthSquared() > 1e-8f;

        if (moved)
        {
            foreach (var (meshId, vertices) in _affectedVertices)
            {
                VertexTransform.Translate(_scene.Meshes.First(m => m.Id == meshId), vertices, delta);
            }
        }

        Reset();
        return moved;
    }

    /// <summary>Cancela a operação em andamento sem alterar as malhas originais. Se uma duplicata já foi
    /// criada (Ctrl), ela permanece na cena parada no ponto original — cancelar não a remove, só não a
    /// desloca; ver limitação na documentação da Etapa 08.</summary>
    public void Cancel() => Reset();

    public IReadOnlyList<Vector3>? GetPreviewSegments()
    {
        if (_affectedVertices is null)
        {
            return null;
        }

        var delta = CurrentDelta;
        var segments = new List<Vector3>();

        foreach (var (meshId, vertices) in _affectedVertices)
        {
            var mesh = _scene.Meshes.FirstOrDefault(m => m.Id == meshId);
            if (mesh is null)
            {
                continue;
            }

            segments.AddRange(GhostGeometry.BuildTransformedEdgeSegments(mesh, vertices, p => p + delta));
        }

        return segments;
    }

    private bool CanDuplicate(Dictionary<Guid, HashSet<int>> byMesh)
    {
        foreach (var (meshId, vertices) in byMesh)
        {
            var mesh = _scene.Meshes.FirstOrDefault(m => m.Id == meshId);
            if (mesh is null || vertices.Count != mesh.Vertices.Count)
            {
                return false;
            }
        }

        return true;
    }

    private Dictionary<Guid, HashSet<int>> DuplicateMeshes(Dictionary<Guid, HashSet<int>> byMesh)
    {
        var result = new Dictionary<Guid, HashSet<int>>();

        foreach (var meshId in byMesh.Keys)
        {
            var original = _scene.Meshes.First(m => m.Id == meshId);
            var clone = new Mesh($"{original.Name} (cópia)", original.Vertices, original.Edges, original.Triangles);
            _scene.Meshes.Add(clone);

            var vertices = new HashSet<int>();
            for (var i = 0; i < clone.Vertices.Count; i++)
            {
                vertices.Add(i);
            }

            result[clone.Id] = vertices;
        }

        return result;
    }

    private void Reset()
    {
        _affectedVertices = null;
        LockedAxisIndex = null;
        _distanceEntry.Clear();
        _currentDelta = Vector3.Zero;
    }
}
