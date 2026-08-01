namespace Structa.Core.Preferences;

/// <summary>
/// Publicado no <see cref="Messaging.IEventAggregator"/> sempre que o tema ativo da aplicação muda.
/// </summary>
public sealed record ThemeChangedEvent(AppThemeVariant Theme);
