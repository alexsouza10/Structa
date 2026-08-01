using Structa.Geometry;

namespace Structa.Editor;

/// <summary>
/// Conteúdo editável da viewport: por enquanto, apenas uma lista de malhas. Ferramentas de
/// desenho/edição (Etapas 05+) vão popular e modificar isto; nesta etapa ela existe só para dar
/// à Seleção algo real para testar o picking.
/// </summary>
public sealed class Scene
{
    public List<Mesh> Meshes { get; } = [];
}
