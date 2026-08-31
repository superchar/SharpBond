using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace SharpBond.Core.Serialization;

public static class PolymorphicSerialization
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { AddDynamicPolymorphism }
        }
    };

    public static string Serialize(object data)
        => JsonSerializer.Serialize(data, JsonSerializerOptions);

    public static T? Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json, JsonSerializerOptions);

    private static void AddDynamicPolymorphism(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }
        
        if (typeInfo.Type != typeof(State) && typeInfo.Type != typeof(Message))
        {
            return;
        }

        typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
        {
            TypeDiscriminatorPropertyName = "$type",
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization
        };

        var derivedTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeInfo.Type.IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false });

        foreach (var type in derivedTypes)
        {
            typeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(type, type.Name));
        }
    }
}