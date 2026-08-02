using System.Numerics;

namespace Structa.Editor.Tools;

/// <summary>
/// Ponto 3D resolvido pelo <see cref="LineTool"/> para a posição atual do cursor, com a origem do
/// snap (para a UI colorir o indicador) e, quando <see cref="Kind"/> é <see cref="LineSnapKind.Endpoint"/>,
/// a referência ao vértice existente que foi encontrado (para reaproveitar o índice em vez de duplicar).
/// </summary>
public readonly record struct LineSnapResult(Vector3 Position, LineSnapKind Kind, Guid? MeshId = null, int? VertexIndex = null);
