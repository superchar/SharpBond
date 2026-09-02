using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using SharpBond.Core.StateHandling;

namespace SharpBond.Core.Tools;

public static class SchemaGenerator
{
    private static readonly JsonSchemaExporterOptions ExporterOptions = new()
    {
        TransformSchemaNode = (context, schemaNode) =>
        {
            if (schemaNode is not JsonObject jsonObject || context.PropertyInfo?.AttributeProvider == null)
            {
                return schemaNode;
            }

            var descriptionAttribute = context.PropertyInfo.AttributeProvider
                .GetCustomAttributes(typeof(DescriptionAttribute), inherit: true)
                .OfType<DescriptionAttribute>()
                .FirstOrDefault();

            if (descriptionAttribute != null)
            {
                jsonObject["description"] = descriptionAttribute.Description;
            }

            return schemaNode;
        }
    };

    public static string GenerateSchema(MethodInfo methodInfo)
    {
        var propertiesNode = new JsonObject();
        var requiredNode = new JsonArray();

        foreach (var parameter in methodInfo.GetParameters())
        {
            if (parameter.ParameterType.IsAssignableFrom(typeof(State)))
            {
                continue;
            }
            
            var parameterSchemaNode =
                JsonSerializerOptions.Default.GetJsonSchemaAsNode(parameter.ParameterType, ExporterOptions);

            if (parameterSchemaNode is JsonObject parameterObject)
            {
                var paramDescription = parameter.GetCustomAttribute<DescriptionAttribute>()?.Description;
                if (!string.IsNullOrEmpty(paramDescription))
                {
                    parameterObject["description"] = paramDescription;
                }

                propertiesNode[parameter.Name!] = parameterObject;
            }

            if (!parameter.HasDefaultValue && Nullable.GetUnderlyingType(parameter.ParameterType) == null)
            {
                requiredNode.Add(parameter.Name!);
            }
        }
        
        var rootSchema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = propertiesNode
        };
        var toolAttribute = methodInfo.GetCustomAttribute<ToolAttribute>();
        if (!string.IsNullOrEmpty(toolAttribute?.Description))
        {
            rootSchema["description"] = toolAttribute.Description;
        }

        if (requiredNode.Count > 0)
        {
            rootSchema["required"] = requiredNode;
        }

        return rootSchema.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}