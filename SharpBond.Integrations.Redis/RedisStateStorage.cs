using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using SharpBond.Core.Abstractions;
using SharpBond.Integrations.Redis.Serialization;
using StackExchange.Redis;

namespace SharpBond.Integrations.Redis;

public class RedisStateStorage(string connectionString) : IStateStorage
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { PolymorphicSerialization.AddDynamicPolymorphism }
        }
    };

    public async Task<TState> GetAsync<TState>(Guid sessionId)
    {
        var database = GetDatabase();
        var stateJson = await database.StringGetAsync(sessionId.ToString());

        return JsonSerializer.Deserialize<TState>((string)stateJson, JsonSerializerOptions);
    }

    public async Task<TState> PutAsync<TState>(Guid sessionId, TState state)
    {
        var database = GetDatabase();
        await database.StringSetAsync(sessionId.ToString(), JsonSerializer.Serialize(state, JsonSerializerOptions));

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