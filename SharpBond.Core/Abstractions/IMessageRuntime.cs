using SharpBond.Core.StateHandling;

namespace SharpBond.Core.Abstractions;

public interface IMessageRuntime
{
    Task<TWaitMessage> SendAndWaitAsync<TMessage, TWaitMessage>(TMessage message, State state)
        where TMessage : Message
        where TWaitMessage : Message;

    Task SendAsync<TMessage>(TMessage message, Guid sessionId) where TMessage : Message;

    Task RegisterAsync<TAgent>(TAgent agent) where TAgent : Agent;
}