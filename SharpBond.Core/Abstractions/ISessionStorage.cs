namespace SharpBond.Core.Abstractions;

public interface ISessionStorage
{
    
    Task<TState> GetAsync<TState>(Guid sessionId);
    
    Task<TState> PutAsync<TState>(Guid sessionId, TState state);
    
    Task DeleteAsync(Guid sessionId);
}