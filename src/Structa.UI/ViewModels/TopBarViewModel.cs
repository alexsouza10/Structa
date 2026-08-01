using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Structa.Core.Preferences;
using Structa.UI.Services;

namespace Structa.UI.ViewModels;

public partial class TopBarViewModel : ViewModelBase
{
    private readonly IThemeService _themeService;
    private bool _suppressThemeToggle;

    [ObservableProperty]
    public partial bool IsDarkTheme { get; set; }

    public TopBarViewModel(IThemeService themeService)
    {
        _themeService = themeService;

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
    private void Exit()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
