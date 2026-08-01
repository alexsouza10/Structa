using System.Numerics;
using Structa.Core.Messaging;
using Structa.Core.Selection;
using Structa.Geometry;

namespace Structa.Selection;

/// <summary>
/// Estado de seleção da cena: mantém os elementos selecionados (vértices, arestas, faces ou
/// objetos, de acordo com <see cref="Mode"/>) e resolve o picking por ray casting. Reage a
/// <see cref="SelectionModeChangedEvent"/> (ex.: troca de modo pela barra lateral) e publica
/// <see cref="SelectionChangedEvent"/> sempre que a seleção muda, via <see cref="IEventAggregator"/>
/// — não há acoplamento direto com a UI.
/// </summary>
public sealed class SelectionManager : IDisposable
{
    private const float VertexPixelTolerance = 10f;
    private const float EdgePixelTolerance = 6f;

    private readonly IEventAggregator _eventAggregator;
    private readonly HashSet<SelectableElement> _selected = [];
    private readonly IDisposable _modeSubscription;

    public SelectionManager(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        _modeSubscription = eventAggregator.Subscribe<SelectionModeChangedEvent>(OnModeChanged);
    }

    public SelectionMode Mode { get; private set; } = SelectionMode.Object;

    public IReadOnlySet<SelectableElement> Selected => _selected;

    public bool IsSelected(SelectableElement element) => _selected.Contains(element);

    public bool IsObjectSelected(Guid meshId) => _selected.Contains(new SelectableElement(meshId, SelectionMode.Object, 0));

    public void Clear()
    {
        if (_selected.Count == 0)
        {
            return;
        }

        _selected.Clear();
        NotifyChanged();
    }

    /// <summary>
    /// Faz o picking no ponto de tela informado e atualiza a seleção de acordo com <see cref="Mode"/>.
    /// <paramref name="additive"/> (Shift/Ctrl) preserva a seleção atual e alterna (toggle) o item clicado.
    /// </summary>
    public bool Pick(
        Vector2 screenPoint,
        Vector2 viewportSize,
        Vector3 cameraPosition,
        Matrix4x4 view,
        Matrix4x4 projection,
        IReadOnlyList<Mesh> meshes,
        bool additive)
    {
        var ray = RayCaster.ScreenPointToRay(screenPoint, viewportSize, cameraPosition, view, projection);

        var hit = Mode switch
        {
            SelectionMode.Vertex => FindClosestVertex(screenPoint, meshes, viewportSize, view, projection),
            SelectionMode.Edge => FindClosestEdge(screenPoint, meshes, viewportSize, view, projection),
            SelectionMode.Face => FindClosestFace(ray, meshes),
            SelectionMode.Object => FindClosestObject(ray, meshes),
            _ => null,
        };

        if (!additive)
        {
            _selected.Clear();
        }

        if (hit is { } element && !_selected.Add(element) && additive)
        {
            _selected.Remove(element);
        }

        NotifyChanged();
        return hit is not null;
    }

    private void OnModeChanged(SelectionModeChangedEvent e)
    {
        if (Mode == e.Mode)
        {
            return;
        }

        Mode = e.Mode;
        Clear();
    }

    private void NotifyChanged() => _eventAggregator.Publish(new SelectionChangedEvent(Mode, _selected.Count));

    private static SelectableElement? FindClosestVertex(
        Vector2 screenPoint, IReadOnlyList<Mesh> meshes, Vector2 viewportSize, Matrix4x4 view, Matrix4x4 projection)
    {
        SelectableElement? best = null;
        var bestDistance = VertexPixelTolerance;

        foreach (var mesh in meshes)
        {
            for (var i = 0; i < mesh.Vertices.Count; i++)
            {
                if (!RayCaster.TryWorldToScreen(mesh.Vertices[i], viewportSize, view, projection, out var screen))
                {
                    continue;
                }

                var distance = Vector2.Distance(screenPoint, screen);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = new SelectableElement(mesh.Id, SelectionMode.Vertex, i);
                }
            }
        }

        return best;
    }

    private static SelectableElement? FindClosestEdge(
        Vector2 screenPoint, IReadOnlyList<Mesh> meshes, Vector2 viewportSize, Matrix4x4 view, Matrix4x4 projection)
    {
        SelectableElement? best = null;
        var bestDistance = EdgePixelTolerance;

        foreach (var mesh in meshes)
        {
            for (var i = 0; i < mesh.Edges.Count; i++)
            {
                var (a, b) = mesh.Edges[i];

                if (!RayCaster.TryWorldToScreen(mesh.Vertices[a], viewportSize, view, projection, out var screenA) ||
                    !RayCaster.TryWorldToScreen(mesh.Vertices[b], viewportSize, view, projection, out var screenB))
                {
                    continue;
                }

                var distance = RayIntersection.PointToSegmentDistance(screenPoint, screenA, screenB);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = new SelectableElement(mesh.Id, SelectionMode.Edge, i);
                }
            }
        }

        return best;
    }

    private static SelectableElement? FindClosestFace(Ray ray, IReadOnlyList<Mesh> meshes)
    {
        SelectableElement? best = null;
        var bestDistance = float.MaxValue;

        foreach (var mesh in meshes)
        {
            for (var i = 0; i < mesh.Triangles.Count; i++)
            {
                var (a, b, c) = mesh.Triangles[i];

                if (RayIntersection.TryIntersectTriangle(ray, mesh.Vertices[a], mesh.Vertices[b], mesh.Vertices[c], out var distance)
                    && distance < bestDistance)
                {
                    bestDistance = distance;
                    best = new SelectableElement(mesh.Id, SelectionMode.Face, i);
                }
            }
        }

        return best;
    }

    private static SelectableElement? FindClosestObject(Ray ray, IReadOnlyList<Mesh> meshes)
    {
        Guid? bestMeshId = null;
        var bestDistance = float.MaxValue;

        foreach (var mesh in meshes)
        {
            foreach (var (a, b, c) in mesh.Triangles)
            {
                if (RayIntersection.TryIntersectTriangle(ray, mesh.Vertices[a], mesh.Vertices[b], mesh.Vertices[c], out var distance)
                    && distance < bestDistance)
                {
                    bestDistance = distance;
                    bestMeshId = mesh.Id;
                }
            }
        }

        return bestMeshId is { } id ? new SelectableElement(id, SelectionMode.Object, 0) : null;
    }

    public void Dispose() => _modeSubscription.Dispose();
}
