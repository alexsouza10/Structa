using FluentValidation;
using MediatR;
using Structa.Core.Preferences;

namespace Structa.Application.Preferences.Commands;

public sealed record SetThemeCommand(AppThemeVariant Theme) : IRequest;

public sealed class SetThemeCommandValidator : AbstractValidator<SetThemeCommand>
{
    public SetThemeCommandValidator()
    {
        RuleFor(x => x.Theme).IsInEnum();
    }
}

internal sealed class SetThemeCommandHandler(IUserPreferencesRepository repository) : IRequestHandler<SetThemeCommand>
{
    public async Task Handle(SetThemeCommand request, CancellationToken cancellationToken)
    {
        var preferences = await repository.GetAsync(cancellationToken) ?? new UserPreferences();
        preferences.Theme = request.Theme;

        await repository.SaveAsync(preferences, cancellationToken);
    }
}
