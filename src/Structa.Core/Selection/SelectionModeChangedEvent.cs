namespace Structa.Core.Selection;

/// <summary>Publicado quando o usuário troca o modo de seleção (ex.: pela barra lateral).</summary>
public sealed record SelectionModeChangedEvent(SelectionMode Mode);
