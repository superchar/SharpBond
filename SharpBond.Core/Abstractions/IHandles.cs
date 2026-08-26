namespace SharpBond.Core.Abstractions;

public interface IHandles<TState, TMessage>
{
    Task<(TState State, List<Message> Messages)> HandleAsync(TState state, TMessage message);
}