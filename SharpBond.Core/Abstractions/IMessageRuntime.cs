namespace SharpBond.Core.Abstractions;

public interface IMessageRuntime
{
    Task SendAsync<TMessage>(TMessage message, State state) where TMessage : Message;

    Task SendAsync<TMessage>(TMessage message, Guid sessionId) where TMessage : Message;

    Task RegisterAsync<TAgent>(TAgent agent) where TAgent : Agent;
}