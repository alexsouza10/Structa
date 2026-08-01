using MediatR;
using Structa.Core.Preferences;

namespace Structa.Application.Preferences.Queries;

public sealed record GetUserPreferencesQuery : IRequest<UserPreferences>;

internal sealed class GetUserPreferencesQueryHandler(IUserPreferencesRepository repository)
    : IRequestHandler<GetUserPreferencesQuery, UserPreferences>
{
    public async Task<UserPreferences> Handle(GetUserPreferencesQuery request, CancellationToken cancellationToken)
    {
        return await repository.GetAsync(cancellationToken) ?? new UserPreferences();
    }
}
