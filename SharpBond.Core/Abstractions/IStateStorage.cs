namespace SharpBond.Core.Abstractions;

public interface IStateStorage
{
    
    Task<TState> GetAsync<TState>(Guid sessionId);
    
    Task<TState> PutAsync<TState>(Guid sessionId, TState state);
    
    Task DeleteAsync(Guid sessionId);
}