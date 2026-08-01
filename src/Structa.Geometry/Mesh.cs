using System.Numerics;

namespace Structa.Geometry;

/// <summary>
/// Topologia lógica de uma malha: vértices e as arestas/triângulos que os conectam, referenciados
/// por índice. É a forma "selecionável" da geometria — a malha usada para desenho (com normais
/// duplicadas por face etc.) é derivada disto pelo <c>MeshRenderer</c>, não faz parte deste tipo.
/// </summary>
public sealed class Mesh
{
    public Guid Id { get; } = Guid.NewGuid();

    public required string Name { get; init; }

    public required IReadOnlyList<Vector3> Vertices { get; init; }

    public required IReadOnlyList<(int A, int B)> Edges { get; init; }

    public required IReadOnlyList<(int A, int B, int C)> Triangles { get; init; }
}
