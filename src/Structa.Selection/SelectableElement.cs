using Structa.Core.Selection;

namespace Structa.Selection;

/// <summary>
/// Identifica um alvo selecionável de forma única: a malha dona, o tipo de elemento e o índice
/// dentro da malha (índice de vértice, de aresta ou de triângulo — ignorado para <see cref="SelectionMode.Object"/>).
/// </summary>
public readonly record struct SelectableElement(Guid MeshId, SelectionMode Kind, int Index);
