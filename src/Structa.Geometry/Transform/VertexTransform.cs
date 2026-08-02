using System.Numerics;

namespace Structa.Geometry.Transform;

/// <summary>
/// Aplica transformações geométricas a um subconjunto dos vértices de uma malha (por índice) — a base
/// das ferramentas Mover, Rotacionar, Escalar e Espelhar (Etapa 08). Não sabe nada de seleção nem de
/// UI: recebe só a malha e os índices a transformar.
/// </summary>
public static class VertexTransform
{
    public static void Translate(Mesh mesh, IEnumerable<int> vertexIndices, Vector3 delta)
    {
        if (delta == Vector3.Zero)
        {
            return;
        }

        foreach (var v in vertexIndices)
        {
            mesh.MoveVertex(v, mesh.Vertices[v] + delta);
        }
    }

    public static void Rotate(Mesh mesh, IEnumerable<int> vertexIndices, Vector3 pivot, Vector3 axis, float angleRadians)
    {
        if (MathF.Abs(angleRadians) < 1e-6f)
        {
            return;
        }

        var rotation = Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), angleRadians);

        foreach (var v in vertexIndices)
        {
            var relative = mesh.Vertices[v] - pivot;
            mesh.MoveVertex(v, pivot + Vector3.Transform(relative, rotation));
        }
    }

    /// <summary>Escala uniforme (mesmo fator nos 3 eixos) em torno de um pivô.</summary>
    public static void Scale(Mesh mesh, IEnumerable<int> vertexIndices, Vector3 pivot, float factor)
    {
        if (MathF.Abs(factor - 1f) < 1e-6f)
        {
            return;
        }

        foreach (var v in vertexIndices)
        {
            var relative = mesh.Vertices[v] - pivot;
            mesh.MoveVertex(v, pivot + (relative * factor));
        }
    }

    /// <summary>
    /// Reflete os vértices num plano e inverte o giro de todo triângulo que usa algum deles — espelhar
    /// troca a lateralidade da geometria; sem inverter o giro, as faces ficariam com a normal para
    /// dentro. Assume que <paramref name="vertexIndices"/> cobre a malha inteira ou um subconjunto
    /// fechado (nenhum triângulo com só parte dos vértices selecionados) — é assim que o
    /// <c>MirrorTool</c> usa, restrito a seleção em modo Objeto.
    /// </summary>
    public static void Mirror(Mesh mesh, IReadOnlySet<int> vertexIndices, Vector3 planePoint, Vector3 planeNormal)
    {
        if (vertexIndices.Count == 0)
        {
            return;
        }

        var normal = Vector3.Normalize(planeNormal);

        foreach (var v in vertexIndices)
        {
            var position = mesh.Vertices[v];
            var distance = Vector3.Dot(position - planePoint, normal);
            mesh.MoveVertex(v, position - (2f * distance * normal));
        }

        for (var i = 0; i < mesh.Triangles.Count; i++)
        {
            var (a, b, c) = mesh.Triangles[i];
            if (vertexIndices.Contains(a) || vertexIndices.Contains(b) || vertexIndices.Contains(c))
            {
                mesh.ReplaceTriangle(i, a, c, b);
            }
        }
    }
}
