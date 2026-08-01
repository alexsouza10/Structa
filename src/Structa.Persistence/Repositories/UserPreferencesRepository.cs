using Microsoft.EntityFrameworkCore;
using Structa.Application.Preferences;
using Structa.Core.Preferences;

namespace Structa.Persistence.Repositories;

internal sealed class UserPreferencesRepository(StructaDbContext dbContext) : IUserPreferencesRepository
{
    public Task<UserPreferences?> GetAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.UserPreferences.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.UserPreferences.OrderBy(x => x.Id).FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            dbContext.UserPreferences.Add(preferences);
        }
        else
        {
            existing.Theme = preferences.Theme;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
