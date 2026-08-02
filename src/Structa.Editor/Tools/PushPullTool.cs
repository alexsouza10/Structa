using System.Linq;
using System.Numerics;
using Structa.Editor;
using Structa.Geometry;
using Structa.Geometry.Faces;
using Structa.Selection;

namespace Structa.Editor.Tools;

/// <summary>
/// Ferramenta Empurrar/Puxar: clique numa face para agarrá-la, arraste ao longo da normal dela para
/// extrudar, solte para confirmar. Enquanto arrasta, dá para digitar um número (Enter confirma com essa
/// distância exata) — o mouse só controla a distância até você começar a digitar; a partir daí o valor
/// digitado manda, até apagar tudo de novo (<see cref="NumericEntryBuffer"/>). Esc cancela sem alterar
/// a malha.
///
/// A malha só é mutada em <see cref="Commit"/> — durante o arrasto nada muda de verdade, quem desenha
/// o "fantasma" da extrusão é a camada de renderização, usando <see cref="GetBoundaryLoopPositions"/> e
/// <see cref="CurrentOffset"/>.
/// </summary>
public sealed class PushPullTool
{
    private readonly Scene _scene;
    private readonly NumericEntryBuffer _distanceEntry = new();

    private Mesh? _mesh;
    private List<int>? _faceGroup;
    private List<int>? _boundaryLoopIndices;
    private Vector3 _normal;
    private Vector3 _origin;
    private float _draggedDistance;

    public PushPullTool(Scene scene) => _scene = scene;

    /// <summary>Verdadeiro entre um clique bem-sucedido numa face (<see cref="TryBegin"/>) e o
    /// commit/cancelamento.</summary>
    public bool IsActive => _mesh is not null;

    /// <summary>Distância efetiva atual: o valor digitado, se houver; senão, a distância arrastada.</summary>
    public float CurrentDistance => _distanceEntry.TryGetValue(out var typed) ? typed : _draggedDistance;

    public Vector3 CurrentOffset => _normal * CurrentDistance;

    /// <summary>Texto digitado até agora (para exibir no indicador da UI), ou nulo se o mouse ainda
    /// estiver no controle.</summary>
    public string? TypedDistanceText => _distanceEntry.Text;

    /// <summary>Tenta agarrar a face sob o ponto de tela informado. Retorna false se não houver face ali.</summary>
    public bool TryBegin(
        Vector2 screenPoint, Vector2 viewportSize, Vector3 cameraPosition, Matrix4x4 view, Matrix4x4 projection)
    {
        var ray = RayCaster.ScreenPointToRay(screenPoint, viewportSize, cameraPosition, view, projection);

        if (!FacePicker.TryFindClosest(ray, _scene.Meshes, out var meshId, out var triangleIndex, out var hitPoint))
        {
            return false;
        }

        var mesh = _scene.Meshes.First(m => m.Id == meshId);
        var group = FaceGroupFinder.FindConnectedCoplanarTriangles(mesh, triangleIndex);
        var boundary = FaceBoundary.FindDirectedBoundaryEdges(mesh, group);

        _mesh = mesh;
        _faceGroup = group;
        _normal = FaceGroupFinder.TriangleNormal(mesh, triangleIndex);
        _origin = hitPoint;
        _draggedDistance = 0f;
        _distanceEntry.Clear();
        _boundaryLoopIndices = FaceBoundary.TryOrderLoop(boundary, out var loop) ? loop : null;

        return true;
    }

    /// <summary>Atualiza a distância arrastada a partir do ponto de tela atual — projeta o cursor na reta
    /// que passa pelo ponto de clique original ao longo da normal da face.</summary>
    public void UpdateDrag(
        Vector2 screenPoint, Vector2 viewportSize, Vector3 cameraPosition, Matrix4x4 view, Matrix4x4 projection)
    {
        if (_mesh is null)
        {
            return;
        }

        var ray = RayCaster.ScreenPointToRay(screenPoint, viewportSize, cameraPosition, view, projection);
        if (RayIntersection.TryClosestPointOnLine(ray, _origin, _normal, out var closest))
        {
            _draggedDistance = Vector3.Dot(closest - _origin, _normal);
        }
    }

    /// <summary>Acrescenta um caractere ao valor de distância digitado (dígitos, ponto decimal, sinal).</summary>
    public void AppendDistanceCharacter(char character)
    {
        if (_mesh is not null)
        {
            _distanceEntry.Append(character);
        }
    }

    public void RemoveLastDistanceCharacter() => _distanceEntry.RemoveLast();

    /// <summary>Confirma a extrusão com a distância efetiva atual. Sem efeito (retorna false) se a
    /// distância for ~zero ou não houver operação em andamento.</summary>
    public bool Commit()
    {
        if (_mesh is null || _faceGroup is null)
        {
            return false;
        }

        var extruded = FaceExtruder.Extrude(_mesh, _faceGroup, _normal, CurrentDistance);
        Reset();
        return extruded;
    }

    /// <summary>Cancela a operação em andamento sem alterar a malha.</summary>
    public void Cancel() => Reset();

    /// <summary>Posições atuais do contorno da face (ordem consistente ao redor do loop), para o preview
    /// desenhar o fantasma da extrusão. Nulo se não houver operação ativa ou o contorno não for um loop
    /// simples (ver limitação em <see cref="FaceBoundary.TryOrderLoop"/>).</summary>
    public IReadOnlyList<Vector3>? GetBoundaryLoopPositions()
    {
        if (_mesh is null || _boundaryLoopIndices is null)
        {
            return null;
        }

        var positions = new Vector3[_boundaryLoopIndices.Count];
        for (var i = 0; i < _boundaryLoopIndices.Count; i++)
        {
            positions[i] = _mesh.Vertices[_boundaryLoopIndices[i]];
        }

        return positions;
    }

    private void Reset()
    {
        _mesh = null;
        _faceGroup = null;
        _boundaryLoopIndices = null;
        _distanceEntry.Clear();
        _draggedDistance = 0f;
    }
}
