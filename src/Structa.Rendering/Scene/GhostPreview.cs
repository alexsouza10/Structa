using System.Numerics;

namespace Structa.Rendering.Scene;

/// <summary>Parâmetros para o <see cref="GhostOutlineRenderer"/> desenhar o "fantasma" de uma operação
/// pendente — Empurrar/Puxar, Mover, Rotacionar ou Escalar (só uma por vez, já que só uma ferramenta
/// fica ativa) — resolvidos pela camada que conhece a ferramenta (UI); este módulo só desenha o que
/// recebe. <see cref="Segments"/> já vem em pares consecutivos (ver <see cref="GhostOutlineRenderer.Render"/>).</summary>
public readonly record struct GhostPreview(IReadOnlyList<Vector3> Segments, Vector3 Color);
