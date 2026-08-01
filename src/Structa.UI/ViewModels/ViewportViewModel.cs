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
}
