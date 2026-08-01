using System.Collections.Concurrent;
using Structa.Core.Messaging;

namespace Structa.Infrastructure.Messaging;

/// <summary>
/// Implementação thread-safe, em memória, de <see cref="IEventAggregator"/>.
/// </summary>
public sealed class EventAggregator : IEventAggregator
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _subscribers = new();

    public void Publish<TEvent>(TEvent @event) where TEvent : notnull
    {
        if (!_subscribers.TryGetValue(typeof(TEvent), out var handlers))
        {
            return;
        }

        Delegate[] snapshot;
        lock (handlers)
        {
            snapshot = [.. handlers];
        }

        foreach (var handler in snapshot)
        {
            ((Action<TEvent>)handler).Invoke(@event);
        }
    }

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : notnull
    {
        var handlers = _subscribers.GetOrAdd(typeof(TEvent), static _ => []);

        lock (handlers)
        {
            handlers.Add(handler);
        }

        return new Subscription(() =>
        {
            lock (handlers)
            {
                handlers.Remove(handler);
            }
        });
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            unsubscribe();
        }
    }
}
