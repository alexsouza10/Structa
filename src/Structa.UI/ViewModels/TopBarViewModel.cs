using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Structa.Core.Editor;
using Structa.Core.Messaging;
using Structa.Core.Preferences;
using Structa.UI.Services;

namespace Structa.UI.ViewModels;

/// <summary>
/// Barra superior: tema, menus e a ferramenta ativa da viewport (Selecionar/Linha). A troca de
/// ferramenta é publicada via <see cref="IEventAggregator"/> — o <c>RenderViewport</c> assina o
/// evento e não tem referência direta a este ViewModel, mesmo padrão do modo de seleção na SideBar.
/// </summary>
public partial class TopBarViewModel : ViewModelBase
{
    private readonly IThemeService _themeService;
    private readonly IEventAggregator _eventAggregator;
    private bool _suppressThemeToggle;

    [ObservableProperty]
    public partial bool IsDarkTheme { get; set; }

    [ObservableProperty]
    public partial EditorTool ActiveTool { get; set; } = EditorTool.Select;

    public TopBarViewModel(IThemeService themeService, IEventAggregator eventAggregator)
    {
        _themeService = themeService;
        _eventAggregator = eventAggregator;

        _suppressThemeToggle = true;
        IsDarkTheme = themeService.CurrentTheme == AppThemeVariant.Dark;
        _suppressThemeToggle = false;
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        if (_suppressThemeToggle)
        {
            return;
        }

        _ = _themeService.SetThemeAsync(value ? AppThemeVariant.Dark : AppThemeVariant.Light);
    }

    [RelayCommand]
    private void SetTool(EditorTool tool) => ActiveTool = tool;

    partial void OnActiveToolChanged(EditorTool value) => _eventAggregator.Publish(new EditorToolChangedEvent(value));

    [RelayCommand]
    private void Exit()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
