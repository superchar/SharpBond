using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using SharpBond.Core;

namespace SharpBond.Integrations.Redis.Serialization;

public static class PolymorphicSerialization
{
    public static void AddDynamicPolymorphism(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type != typeof(State))
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