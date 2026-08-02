using CommunityToolkit.Mvvm.ComponentModel;

namespace Structa.UI.ViewModels;

/// <summary>
/// Representa a área de desenho 3D. A partir da Etapa 02 hospeda o <see cref="Views.RenderViewport"/>
/// (engine OpenGL/Silk.NET); esta classe expõe apenas o estado observável pela View (FPS).
/// </summary>
public sealed partial class ViewportViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial double Fps { get; set; }

    /// <summary>Rótulo da ferramenta de arrasto ativa (distância do Empurrar/Puxar ou Mover, ângulo do
    /// Rotacionar, fator do Escalar), já formatado; nulo quando nenhuma está ativa. Alimentado pelo
    /// <c>RenderViewport</c> via binding OneWayToSource.</summary>
    [ObservableProperty]
    public partial string? ToolStatusText { get; set; }
}
