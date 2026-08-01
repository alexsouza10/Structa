using Structa.Core.Preferences;

namespace Structa.Application.Preferences;

/// <summary>
/// Porta (Repository Pattern) implementada pela camada de Persistence.
/// </summary>
public interface IUserPreferencesRepository
{
    Task<UserPreferences?> GetAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
}
