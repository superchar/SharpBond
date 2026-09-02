using System.Collections.Concurrent;
using System.Threading.Channels;
using SharpBond.Core.Abstractions;
using SharpBond.Core.Helpers;
using SharpBond.Core.Llm;
using SharpBond.Core.StateHandling;

namespace SharpBond.Core;

public abstract class Agent
{
    private readonly IMessageRuntime _messageRuntime;
    private readonly IStateStorage _stateStorage;
    
    private readonly ConcurrentDictionary<Guid, Channel<object>> _channels = new();

    protected Agent(IStateStorage stateStorage, IMessageRuntime messageRuntime, ILlm llm)
    {
        _stateStorage = stateStorage;
        _messageRuntime = messageRuntime;
        _messageRuntime.RegisterAsync(this);
    }

    public void QueueMessage<TMessage>(TMessage message, Guid sessionId)
    {
        if (_channels.TryGetValue(sessionId, out var channel))
        {
            channel.Writer.TryWrite(message);
        }
        else
        {   channel = Channel.CreateUnbounded<object>();
            channel.Writer.TryWrite(message);
            _ = ChannelWorker(channel, sessionId);
            _channels.TryAdd(sessionId, channel);
        }
    }

    private async Task ChannelWorker(Channel<object> channel, Guid sessionId)
    {
        await foreach (var message in channel.Reader.ReadAllAsync())
        {
            var state = await _stateStorage.GetAsync<State>(sessionId);
            var (newState, messages) = await GetType().CallHandleMethodAsync(this, message, state);
            
            await _stateStorage.PutAsync(state.SessionId, newState);

            foreach (var newMessage in messages)
            {
                await _messageRuntime.SendAsync(newMessage, newState.SessionId);
            }
        }
    }
}
