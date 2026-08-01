using Microsoft.Extensions.DependencyInjection;
using Structa.UI.Services;
using Structa.UI.ViewModels;
using Structa.UI.Views;

namespace Structa.UI;

public static class DependencyInjection
{
    public static IServiceCollection AddStructaUI(this IServiceCollection services)
    {
        services.AddSingleton<IThemeService, ThemeService>();

        services.AddSingleton<TopBarViewModel>();
        services.AddSingleton<SideBarViewModel>();
        services.AddSingleton<ViewportViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        services.AddSingleton(sp => new MainWindow
        {
            DataContext = sp.GetRequiredService<MainWindowViewModel>(),
        });

        return services;
    }
}
