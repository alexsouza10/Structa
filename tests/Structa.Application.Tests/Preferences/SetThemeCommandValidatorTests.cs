using Structa.Application.Preferences.Commands;
using Structa.Core.Preferences;

namespace Structa.Application.Tests.Preferences;

public class SetThemeCommandValidatorTests
{
    private readonly SetThemeCommandValidator _validator = new();

    [Theory]
    [InlineData(AppThemeVariant.Light)]
    [InlineData(AppThemeVariant.Dark)]
    public void Valid_theme_passes_validation(AppThemeVariant theme)
    {
        var result = _validator.Validate(new SetThemeCommand(theme));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Undefined_theme_fails_validation()
    {
        var result = _validator.Validate(new SetThemeCommand((AppThemeVariant)999));

        Assert.False(result.IsValid);
    }
}
