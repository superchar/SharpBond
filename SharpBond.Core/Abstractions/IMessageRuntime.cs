namespace SharpBond.Core.Abstractions;

public interface IMessageRuntime
{
    Task SendAsync<TMessage>(TMessage message, State state);
    
    Task SendAsync<TMessage>(TMessage message, Guid sessionId);
    
    Task RegisterAsync<TAgent>(TAgent agent) where TAgent : Agent;
}