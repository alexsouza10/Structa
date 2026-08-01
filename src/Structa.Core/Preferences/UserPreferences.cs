namespace Structa.Core.Preferences;

/// <summary>
/// Preferências de usuário persistidas localmente (linha única, Id fixo).
/// </summary>
public sealed class UserPreferences
{
    public int Id { get; set; } = 1;

    public AppThemeVariant Theme { get; set; } = AppThemeVariant.Light;
}
