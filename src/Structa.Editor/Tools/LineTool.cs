using System.Numerics;
using Structa.Editor;
using Structa.Geometry;
using Structa.Geometry.Faces;
using Structa.Selection;

namespace Structa.Editor.Tools;

/// <summary>
/// Ferramenta Linha: dois cliques criam uma aresta. O resultado do primeiro clique vira o ponto
/// inicial do próximo segmento automaticamente (encadeamento, como no SketchUp), até <see cref="Cancel"/>
/// ser chamado (Esc) ou a ferramenta ser trocada.
///
/// Resolução do ponto sob o cursor, em ordem de prioridade:
/// 1. Ponta de uma aresta existente (qualquer malha da cena) dentro da tolerância em pixels — permite
///    fechar loops e conectar a geometria já desenhada.
/// 2. Um dos eixos X/Y/Z partindo do ponto inicial, se o cursor estiver perto o bastante dele em tela
///    (inferência de eixo, como no SketchUp).
/// 3. Projeção livre no plano de referência: chão (Z=0) para o primeiro ponto, ou o plano nivelado com
///    o ponto inicial para o segundo (mantém o segmento na horizontal por padrão).
///
/// Toda a geometria desenhada por esta ferramenta acumula em uma única malha "de esboço" por instância
/// de <see cref="LineTool"/> — vértices compartilhados dentro dela são reaproveitados por índice (solda),
/// o que permite ao <see cref="FaceDetector"/> reconhecer loops fechados: toda vez que uma aresta é
/// adicionada, ele tenta criar a face automaticamente, como no SketchUp.
/// </summary>
public sealed class LineTool
{
    private const float EndpointPixelTolerance = 12f;
    private const float AxisPixelTolerance = 10f;

    // Distância (ao quadrado) abaixo da qual um segundo clique é tratado como "mesmo ponto" do início
    // do segmento — evita arestas de comprimento ~zero e vértices duplicados coincidentes por um
    // clique repetido sem mover o cursor.
    private const float WeldDistanceSquared = 1e-6f;

    private static readonly LineSnapKind[] AxisKindByIndex = [LineSnapKind.AxisX, LineSnapKind.AxisY, LineSnapKind.AxisZ];

    private readonly Scene _scene;
    private Mesh? _sketchMesh;
    private Vector3? _startPoint;
    private (Guid MeshId, int VertexIndex)? _startVertexRef;

    public LineTool(Scene scene) => _scene = scene;

    /// <summary>Verdadeiro entre o primeiro clique de um segmento e o segundo (ou o cancelamento).</summary>
    public bool IsDrawing => _startPoint is not null;

    public Vector3? StartPoint => _startPoint;

    /// <summary>Resolve o ponto sob o cursor sem alterar o estado da ferramenta — usado para desenhar o preview.</summary>
    public LineSnapResult Preview(
        Vector2 screenPoint, Vector2 viewportSize, Vector3 cameraPosition, Matrix4x4 view, Matrix4x4 projection) =>
        ResolveSnap(screenPoint, viewportSize, cameraPosition, view, projection);

    /// <summary>
    /// Clique na ferramenta: se não havia ponto inicial, apenas o define; caso contrário, cria a aresta
    /// entre o início e o ponto atual e encadeia o próximo segmento a partir dele.
    /// </summary>
    public void Click(
        Vector2 screenPoint, Vector2 viewportSize, Vector3 cameraPosition, Matrix4x4 view, Matrix4x4 projection)
    {
        var snap = ResolveSnap(screenPoint, viewportSize, cameraPosition, view, projection);

        if (_startPoint is not { } start)
        {
            BeginAt(snap);
            return;
        }

        // O próximo segmento começa exatamente no vértice de malha de esboço que acabou de ser
        // resolvido (criado ou reaproveitado) para o fim deste — não no resultado bruto do snap.
        // Se usássemos o snap bruto aqui, um ponto livre (Plano/Eixo) reencadeado perderia o índice
        // do vértice recém-criado e ganharia um duplicado coincidente no próximo clique, quebrando a
        // solda entre segmentos consecutivos de que a Etapa 06 (detecção de faces) vai depender.
        var end = CommitSegment(start, _startVertexRef, snap);
        _startPoint = snap.Position;
        _startVertexRef = end;
    }

    /// <summary>Cancela o segmento pendente (Esc). A ferramenta continua ativa para um novo desenho.</summary>
    public void Cancel()
    {
        _startPoint = null;
        _startVertexRef = null;
    }

    private void BeginAt(LineSnapResult snap)
    {
        _startPoint = snap.Position;
        _startVertexRef = snap.Kind == LineSnapKind.Endpoint ? (snap.MeshId!.Value, snap.VertexIndex!.Value) : null;
    }

    private (Guid MeshId, int VertexIndex) CommitSegment(
        Vector3 startPosition, (Guid MeshId, int VertexIndex)? startRef, LineSnapResult end)
    {
        var mesh = _sketchMesh ??= CreateSketchMesh();

        var startIndex = ResolveVertexIndex(mesh, startPosition, startRef);

        var endRef = end.Kind == LineSnapKind.Endpoint ? (end.MeshId!.Value, end.VertexIndex!.Value) : ((Guid, int)?)null;
        var endIndex = endRef is null && Vector3.DistanceSquared(startPosition, end.Position) < WeldDistanceSquared
            ? startIndex
            : ResolveVertexIndex(mesh, end.Position, endRef);

        if (startIndex != endIndex && !mesh.HasEdge(startIndex, endIndex))
        {
            mesh.AddEdge(startIndex, endIndex);
            FaceDetector.TryDetectFace(mesh, startIndex, endIndex);
        }

        return (mesh.Id, endIndex);
    }

    private Mesh CreateSketchMesh()
    {
        var mesh = new Mesh("Desenho");
        _scene.Meshes.Add(mesh);
        return mesh;
    }

    private static int ResolveVertexIndex(Mesh mesh, Vector3 position, (Guid MeshId, int VertexIndex)? reference) =>
        reference is { } r && r.MeshId == mesh.Id ? r.VertexIndex : mesh.AddVertex(position);

    private LineSnapResult ResolveSnap(
        Vector2 screenPoint, Vector2 viewportSize, Vector3 cameraPosition, Matrix4x4 view, Matrix4x4 projection)
    {
        if (EndpointSnapper.TryFindClosest(
                screenPoint, _scene.Meshes, viewportSize, view, projection, EndpointPixelTolerance,
                out var meshId, out var vertexIndex, out var endpointPosition))
        {
            return new LineSnapResult(endpointPosition, LineSnapKind.Endpoint, meshId, vertexIndex);
        }

        var ray = RayCaster.ScreenPointToRay(screenPoint, viewportSize, cameraPosition, view, projection);

        if (_startPoint is { } start &&
            AxisSnapper.TryFindClosestAxis(ray, start, screenPoint, viewportSize, view, projection, AxisPixelTolerance, out var axisPoint, out var axisIndex))
        {
            return new LineSnapResult(axisPoint, AxisKindByIndex[axisIndex]);
        }

        var planeHeight = _startPoint?.Z ?? 0f;
        if (RayIntersection.TryIntersectPlane(ray, new Vector3(0f, 0f, planeHeight), Vector3.UnitZ, out var planePoint))
        {
            return new LineSnapResult(planePoint, LineSnapKind.Plane);
        }

        // Câmera olhando quase paralela ao plano de referência: cai para um plano de frente para ela,
        // passando pelo ponto inicial (ou pela origem), para nunca deixar o cursor sem um ponto válido.
        var fallbackReference = _startPoint ?? Vector3.Zero;
        var cameraForward = Vector3.Normalize(fallbackReference - cameraPosition);
        RayIntersection.TryIntersectPlane(ray, fallbackReference, cameraForward, out var fallbackPoint);
        return new LineSnapResult(fallbackPoint, LineSnapKind.Plane);
    }
}
