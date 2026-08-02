using System.Numerics;

namespace Structa.Rendering.Scene;

/// <summary>Parâmetros para o <see cref="LinePreviewRenderer"/>, resolvidos pela camada que conhece a
/// ferramenta ativa (UI) a partir do <c>LineTool</c> — este módulo só desenha o que recebe.</summary>
public readonly record struct LinePreview(Vector3? SegmentStart, Vector3 CursorPoint, Vector3 MarkerColor, Vector3 LineColor);
