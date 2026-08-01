using Structa.Core.Preferences;

namespace Structa.Core.Tests.Preferences;

public class UserPreferencesTests
{
    [Fact]
    public void Default_theme_is_light()
    {
        var preferences = new UserPreferences();

        Assert.Equal(AppThemeVariant.Light, preferences.Theme);
    }
}
