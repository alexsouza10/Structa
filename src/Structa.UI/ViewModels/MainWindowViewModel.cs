namespace Structa.UI.ViewModels;

public sealed class MainWindowViewModel(TopBarViewModel topBar, SideBarViewModel sideBar, ViewportViewModel viewport)
    : ViewModelBase
{
    public TopBarViewModel TopBar { get; } = topBar;

    public SideBarViewModel SideBar { get; } = sideBar;

    public ViewportViewModel Viewport { get; } = viewport;
}
