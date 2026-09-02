using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using SharpBond.Core.StateHandling;

namespace SharpBond.Core.Tools;

public static class ToolExecutor
{
    public static async Task<string> ExecuteToolAsync(MethodInfo toolMethod, JsonObject parametersJson, object toolObject, State state)
    {
        var parameters = new List<object>();

        foreach (var parameter in toolMethod.GetParameters())
        {
            if (parameter.ParameterType.IsAssignableTo(typeof(State)))
            {
                parameters.Add(state);
            }
            else if (parametersJson.ContainsKey(parameter.Name))
            {
                parameters.Add(parametersJson[parameter.Name].Deserialize(parameter.ParameterType));
            }
        }
        
        var result = toolMethod.Invoke(toolObject, parameters.ToArray());
        if (result is not Task task)
        {
            return JsonSerializer.Serialize(result);
        }
        
        await task;
        var resultProperty = task.GetType().GetProperty("Result").GetValue(task);
        return JsonSerializer.Serialize(resultProperty);

    }
}