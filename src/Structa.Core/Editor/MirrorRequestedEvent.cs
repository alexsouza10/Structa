namespace Structa.Core.Editor;

/// <summary>Publicado quando o usuário pede para espelhar a seleção atual (ex.: botão na barra
/// lateral) — comando instantâneo, não uma troca de ferramenta.</summary>
public sealed record MirrorRequestedEvent(MirrorAxis Axis);
