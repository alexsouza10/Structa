namespace Structa.Core.Editor;

/// <summary>Publicado quando o usuário troca a ferramenta ativa (ex.: pela barra superior).</summary>
public sealed record EditorToolChangedEvent(EditorTool Tool);
