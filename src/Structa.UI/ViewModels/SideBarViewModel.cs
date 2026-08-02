using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Structa.Core.Editor;
using Structa.Core.Messaging;
using Structa.Core.Selection;

namespace Structa.UI.ViewModels;

/// <summary>
/// Painel lateral (Entidades, Materiais, Tags, Cenas). A aba Entidades controla o modo de seleção
/// (vértice/aresta/face/objeto), mostra um resumo da seleção atual e dispara o comando Espelhar (que
/// age sobre a seleção, não é uma ferramenta de arrasto — por isso vive aqui, não na barra superior),
/// comunicando-se com o <c>RenderViewport</c> via <see cref="IEventAggregator"/> — nenhuma referência
/// direta entre eles.
/// </summary>
public sealed partial class SideBarViewModel : ViewModelBase
{
    private readonly IEventAggregator _eventAggregator;

    [ObservableProperty]
    public partial SelectionMode Mode { get; set; } = SelectionMode.Object;

    [ObservableProperty]
    public partial string ModeLabel { get; set; } = "Modo atual: Objeto";

    [ObservableProperty]
    public partial string SelectionSummary { get; set; } = "Nada selecionado";

    public SideBarViewModel(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        eventAggregator.Subscribe<SelectionChangedEvent>(OnSelectionChanged);
    }

    [RelayCommand]
    private void SetMode(SelectionMode mode)
    {
        Serilog.Log.Information("SetMode command invoked with {Mode}", mode);
        Mode = mode;
    }

    [RelayCommand]
    private void Mirror(MirrorAxis axis) => _eventAggregator.Publish(new MirrorRequestedEvent(axis));

    partial void OnModeChanged(SelectionMode value)
    {
        ModeLabel = $"Modo atual: {ModeName(value)}";
        _eventAggregator.Publish(new SelectionModeChangedEvent(value));
    }

    private static string ModeName(SelectionMode mode) => mode switch
    {
        SelectionMode.Vertex => "Vértice",
        SelectionMode.Edge => "Aresta",
        SelectionMode.Face => "Face",
        SelectionMode.Object => "Objeto",
        _ => mode.ToString(),
    };

    private void OnSelectionChanged(SelectionChangedEvent e) => SelectionSummary = Describe(e.Mode, e.Count);

    private static string Describe(SelectionMode mode, int count)
    {
        if (count == 0)
        {
            return "Nada selecionado";
        }

        var (singular, plural, feminine) = mode switch
        {
            SelectionMode.Vertex => ("vértice", "vértices", false),
            SelectionMode.Edge => ("aresta", "arestas", true),
            SelectionMode.Face => ("face", "faces", true),
            SelectionMode.Object => ("objeto", "objetos", false),
            _ => ("item", "itens", false),
        };

        var participle = feminine ? "selecionada" : "selecionado";

        return count == 1
            ? $"1 {singular} {participle}"
            : $"{count} {plural} {participle}s";
    }
}
