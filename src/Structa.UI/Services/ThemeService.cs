using Avalonia.Styling;
using MediatR;
using Microsoft.Extensions.Logging;
using Structa.Application.Preferences.Commands;
using Structa.Application.Preferences.Queries;
using Structa.Core.Messaging;
using Structa.Core.Preferences;

namespace Structa.UI.Services;

public sealed class ThemeService(
    IMediator mediator,
    IEventAggregator eventAggregator,
    ILogger<ThemeService> logger) : IThemeService
{
    public AppThemeVariant CurrentTheme { get; private set; } = AppThemeVariant.Light;

    public async Task InitializeAsync()
    {
        var preferences = await mediator.Send(new GetUserPreferencesQuery());
        Apply(preferences.Theme);
    }

    public async Task SetThemeAsync(AppThemeVariant theme)
    {
        await mediator.Send(new SetThemeCommand(theme));
        Apply(theme);
    }

    private void Apply(AppThemeVariant theme)
    {
        CurrentTheme = theme;

        if (Avalonia.Application.Current is { } app)
        {
            app.RequestedThemeVariant = theme == AppThemeVariant.Dark ? ThemeVariant.Dark : ThemeVariant.Light;
        }

        eventAggregator.Publish(new ThemeChangedEvent(theme));
        logger.LogInformation("Tema aplicado: {Theme}", theme);
    }
}
