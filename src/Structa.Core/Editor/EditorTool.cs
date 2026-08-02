namespace Structa.Core.Editor;

/// <summary>Ferramenta ativa na viewport. <see cref="Select"/> é o padrão (picking via <c>SelectionManager</c>).</summary>
public enum EditorTool
{
    Select,
    Line,
    PushPull,
    Move,
    Rotate,
    Scale,
}
