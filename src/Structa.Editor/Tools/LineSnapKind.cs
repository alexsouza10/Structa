namespace Structa.Editor.Tools;

/// <summary>Origem do ponto resolvido pelo <see cref="LineTool"/> para o cursor atual.</summary>
public enum LineSnapKind
{
    /// <summary>Grudado em um vértice existente (de qualquer malha da cena).</summary>
    Endpoint,

    /// <summary>Travado no eixo X a partir do ponto inicial.</summary>
    AxisX,

    /// <summary>Travado no eixo Y a partir do ponto inicial.</summary>
    AxisY,

    /// <summary>Travado no eixo Z a partir do ponto inicial.</summary>
    AxisZ,

    /// <summary>Livre, projetado no plano de referência (chão ou nível do ponto inicial).</summary>
    Plane,
}
