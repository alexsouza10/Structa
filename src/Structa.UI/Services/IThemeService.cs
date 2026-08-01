using Structa.Core.Preferences;

namespace Structa.UI.Services;

public interface IThemeService
{
    AppThemeVariant CurrentTheme { get; }

    /// <summary>
    /// Carrega a preferência persistida e aplica o tema correspondente. Deve ser chamado
    /// uma vez, antes da janela principal ser exibida.
    /// </summary>
    Task InitializeAsync();

    Task SetThemeAsync(AppThemeVariant theme);
}
