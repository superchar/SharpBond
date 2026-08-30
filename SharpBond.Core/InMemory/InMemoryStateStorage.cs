using System.Collections.Concurrent;
using SharpBond.Core.Abstractions;

namespace SharpBond.Core.InMemory;

public class InMemoryStateStorage : IStateStorage
{
    private readonly ConcurrentDictionary<Guid, object> _sessions = new();

    public Task<TState> GetAsync<TState>(Guid sessionId)
        => Task.FromResult((TState)_sessions[sessionId]);

    public Task<TState> PutAsync<TState>(Guid sessionId, TState state)
    {
        _sessions[sessionId] = state;
        return Task.FromResult((TState)_sessions[sessionId]);
    }

    public Task DeleteAsync(Guid sessionI)
    {
        _sessions.TryRemove(sessionI, out _);
        return Task.CompletedTask;
    }
}