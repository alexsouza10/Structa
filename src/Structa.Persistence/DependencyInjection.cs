using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Structa.Application.Preferences;
using Structa.Persistence.Repositories;

namespace Structa.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddStructaPersistence(this IServiceCollection services)
    {
        var dbPath = ResolveDatabasePath();

        services.AddDbContext<StructaDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));

        services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();

        return services;
    }

    private static string ResolveDatabasePath()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Structa");

        Directory.CreateDirectory(folder);

        return Path.Combine(folder, "structa.db");
    }
}
