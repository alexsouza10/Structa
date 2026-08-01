namespace Structa.Core.Messaging;

/// <summary>
/// Barramento de eventos in-process usado para desacoplar módulos (Editor, UI, Persistence etc.)
/// que não devem depender diretamente uns dos outros.
/// </summary>
public interface IEventAggregator
{
    void Publish<TEvent>(TEvent @event) where TEvent : notnull;

    /// <summary>
    /// Registra um handler para <typeparamref name="TEvent"/>. Descarte o retorno para cancelar a inscrição.
    /// </summary>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : notnull;
}
