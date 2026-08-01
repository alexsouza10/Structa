namespace Structa.Core.Selection;

/// <summary>Publicado sempre que o conjunto selecionado muda, para a UI refletir o estado atual.</summary>
public sealed record SelectionChangedEvent(SelectionMode Mode, int Count);
