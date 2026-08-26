using System.Collections.Concurrent;
using SharpBond.Core.Abstractions;
using SharpBond.Core.Helpers;

namespace SharpBond.Core.InMemory;

public class InMemoryMessageRuntime(ISessionStorage sessionStorage) : IMessageRuntime
{
    private readonly ConcurrentDictionary<Type, List<Agent>> _agentRegistry = [];
    private readonly ConcurrentDictionary<(Type, Guid SessionId), TaskCompletionSource<object>> _waiters = [];

    public Task SendAsync<TMessage>(TMessage message, State state) where TMessage : Message
    {
        var messageType = message.GetType();
        if (_waiters.TryGetValue((messageType, state.SessionId), out var taskCompletionSource))
        {
            taskCompletionSource.SetResult(message);
            _waiters.Remove((messageType, state.SessionId), out _);
        }

        if (!_agentRegistry.TryGetValue(message.GetType(), out var agents))
        {
            return Task.CompletedTask;
        }

        foreach (var agent in agents)
        {
            agent.QueueMessage(message, state);
        }

        return Task.CompletedTask;
    }

    public async Task<TWaitMessage> SendAndWaitAsync<TMessage, TWaitMessage>(TMessage message, State state)
        where TMessage : Message
        where TWaitMessage : Message
    {
        var waitTask = WaitAsync<TWaitMessage>(state.SessionId);
        await SendAsync(message, state);

        return await waitTask;
    }

    public async Task SendAsync<TMessage>(TMessage message, Guid sessionId) where TMessage : Message
    {
        var state = await sessionStorage.GetAsync<State>(sessionId);
        await SendAsync(message, state);
    }

    public Task RegisterAsync<TAgent>(TAgent agent) where TAgent : Agent
    {
        var handledMessages =
            agent
                .GetType()
                .GetHandledInterfaces()
                .Select(i => i.GetGenericArguments()[1])
                .ToList();
        foreach (var handledMessage in handledMessages)
        {
            if (_agentRegistry.TryGetValue(handledMessage, out var agents))
            {
                agents.Add(agent);
            }
            else
            {
                _agentRegistry[handledMessage] = [agent];
            }
        }

        return Task.CompletedTask;
    }

    private async Task<TMessage> WaitAsync<TMessage>(Guid sessionId) where TMessage : Message
    {
        var taskCompletionSource = new TaskCompletionSource<object>();
        _waiters[(typeof(TMessage), sessionId)] = taskCompletionSource;
        var result = await taskCompletionSource.Task;
        return (TMessage)result;
    }
}