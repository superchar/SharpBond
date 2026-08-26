using System.Collections;
using System.Collections.Concurrent;
using System.Threading.Channels;
using SharpBond.Core.Abstractions;
using SharpBond.Core.Helpers;

namespace SharpBond.Core;

public abstract class Agent
{
    private readonly IMessageRuntime _messageRuntime;
    private readonly ISessionStorage _sessionStorage;
    
    private readonly ConcurrentDictionary<Guid, Channel<object>> _channels = new();

    protected Agent(ISessionStorage sessionStorage, IMessageRuntime messageRuntime)
    {
        _sessionStorage = sessionStorage;
        _messageRuntime = messageRuntime;
        _messageRuntime.RegisterAsync(this);
    }

    internal void QueueMessage<TMessage>(TMessage message, State state)
    {
        if (_channels.TryGetValue(state.SessionId, out var channel))
        {
            channel.Writer.TryWrite(message);
        }
        else
        {   channel = Channel.CreateUnbounded<object>();
            channel.Writer.TryWrite(message);
            _ = ChannelWorker(channel, state);
            _channels.TryAdd(state.SessionId, channel);
            
        }
    }

    private async Task ChannelWorker(Channel<object> channel, State state)
    {
        await foreach (var message in channel.Reader.ReadAllAsync())
        {
            var (newState, messages) = await GetType().CallHandleMethodAsync(this, message, state);
            
            await _sessionStorage.PutAsync(state.SessionId, newState);

            foreach (var newMessage in messages)
            {
                await _messageRuntime.SendAsync(newMessage, newState.SessionId);
            }
        }
    }
}
