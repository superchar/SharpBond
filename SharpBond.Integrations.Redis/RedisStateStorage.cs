using SharpBond.Core.Abstractions;
using SharpBond.Core.Serialization;
using StackExchange.Redis;

namespace SharpBond.Integrations.Redis;

public class RedisStateStorage(string connectionString) : IStateStorage
{
    public async Task<TState> GetAsync<TState>(Guid sessionId)
    {
        var database = GetDatabase();
        var stateJson = await database.StringGetAsync(sessionId.ToString());

        return PolymorphicSerialization.Deserialize<TState>(stateJson);
    }

    public async Task<TState> PutAsync<TState>(Guid sessionId, TState state)
    {
        var database = GetDatabase();
        await database.StringSetAsync(sessionId.ToString(), PolymorphicSerialization.Serialize(state));

        return state;
    }

    public async Task DeleteAsync(Guid sessionId)
    {
        var database = GetDatabase();
        await database.KeyDeleteAsync(sessionId.ToString());
    }

    private IDatabase GetDatabase()
        => ConnectionMultiplexer.Connect(connectionString).GetDatabase();
}