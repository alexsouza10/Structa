using System.Globalization;

namespace Structa.Editor.Tools;

/// <summary>
/// Buffer de texto para entrada numérica durante um arrasto (distância, ângulo, fator de escala):
/// dígitos/ponto/sinal digitados sobrepõem o valor calculado a partir do mouse, até apagar tudo de
/// novo. Compartilhado por qualquer ferramenta de arrasto com controle por valor exato — Empurrar/Puxar
/// (Etapa 07), Mover, Rotacionar e Escalar (Etapa 08).
/// </summary>
public sealed class NumericEntryBuffer
{
    public string? Text { get; private set; }

    public void Append(char character)
    {
        if (character is not (>= '0' and <= '9') && character != '.' && character != '-')
        {
            return;
        }

        Text = (Text ?? string.Empty) + character;
    }

    public void RemoveLast()
    {
        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        Text = Text.Length > 1 ? Text[..^1] : null;
    }

    public void Clear() => Text = null;

    public bool TryGetValue(out float value)
    {
        value = 0f;
        return !string.IsNullOrEmpty(Text) &&
            float.TryParse(Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
