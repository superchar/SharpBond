using System.Collections.Concurrent;
using SharpBond.Core.Abstractions;
using SharpBond.Core.Helpers;

namespace SharpBond.Core.InMemory;

public class InMemoryMessageRuntime : IMessageRuntime
{
    private readonly ISessionStorage _sessionStorage;
    private readonly ConcurrentDictionary<Type, List<Agent>> _agentRegistry = new();

    public InMemoryMessageRuntime(ISessionStorage sessionStorage)
    {
        _sessionStorage = sessionStorage;
    }

    public Task SendAsync<TMessage>(TMessage message, State state)
    {
        if (!_agentRegistry.TryGetValue(message.GetType(), out var agents))
        {
            return Task.CompletedTask;
            ;
        }

        foreach (var agent in agents)
        {
            agent.QueueMessage(message, state);
        }

        return Task.CompletedTask;
    }

    public async Task SendAsync<TMessage>(TMessage message, Guid sessionId)
    {
        var state = await _sessionStorage.GetAsync<State>(sessionId);
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
}