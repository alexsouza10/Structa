namespace Structa.Rendering.Scene;

/// <summary>
/// Subconjunto de uma malha a destacar visualmente (índices locais a essa malha). Construído pela
/// camada que conhece a seleção (UI) a partir do <c>SelectionManager</c> — este módulo não conhece
/// o tipo de seleção, só desenha os índices que recebe.
/// </summary>
public sealed class MeshHighlight
{
    public static readonly MeshHighlight Empty = new();

    public bool WholeObject { get; init; }

    public IReadOnlyCollection<int> Vertices { get; init; } = [];

    public IReadOnlyCollection<int> Edges { get; init; } = [];

    public IReadOnlyCollection<int> Faces { get; init; } = [];
}
