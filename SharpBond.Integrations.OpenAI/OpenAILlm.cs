using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using OpenAI.Chat;
using SharpBond.Core.Abstractions;
using SharpBond.Core.Helpers;
using SharpBond.Core.Tools;

namespace SharpBond.Integrations.OpenAI;

public class OpenAILlm(string model, string apiKey, object[]? tools = null) : ILlm
{
    private readonly ChatClient _chatClient = new(model, apiKey);
    private readonly ConcurrentDictionary<string, (MethodInfo Method, object TargetObject)> _toolMethods = new();

    public async Task<string> GenerateAsync(string prompt)
    {
        var completionsOptions = new ChatCompletionOptions();
        foreach (var tool in GetTools())
        {
            completionsOptions.Tools.Add(tool);
        }

        var messages = new List<ChatMessage> { new UserChatMessage(prompt) };

        while (true)
        {
            var result = await _chatClient.CompleteChatAsync(messages, completionsOptions);

            if (!result.Value.ToolCalls.Any())
            {
                return result.Value.Content[0].Text;
            }

            messages = await ExecuteToolsAsync(messages, result.Value.ToolCalls);
        }
    }

    public async IAsyncEnumerable<string> GenerateStreamingAsync(string prompt, CancellationToken cancellationToken)
    {
        var completionsOptions = new ChatCompletionOptions();
        foreach (var tool in GetTools())
        {
            completionsOptions.Tools.Add(tool);
        }

        var messages = new List<ChatMessage> { new UserChatMessage(prompt) };

        while (true)
        {
            var result = _chatClient.CompleteChatStreamingAsync(messages, completionsOptions, cancellationToken);

            var currentToolName = string.Empty;
            var currentToolCallId = string.Empty;
            var toolCalls = new List<ChatToolCall>();
            var toolCallArgumentsStringBuilder = new StringBuilder();
            await foreach (var token in result)
            {
                if (!token.ToolCallUpdates.Any())
                {
                    if (token.ContentUpdate.Count == 0)
                    {
                        continue;
                    }
                    
                    yield return token.ContentUpdate[0].Text;
                }

                foreach (var toolCallUpdate in token.ToolCallUpdates)
                {
                    if (string.IsNullOrEmpty(currentToolName))
                    {
                        currentToolName = toolCallUpdate.FunctionName;
                        currentToolCallId = toolCallUpdate.ToolCallId;
                    }
                    
                    if (toolCallUpdate.FunctionName == currentToolName || string.IsNullOrEmpty(toolCallUpdate.FunctionName))
                    {
                        toolCallArgumentsStringBuilder.Append(toolCallUpdate.FunctionArgumentsUpdate);
                        continue;
                    }
                    
                    toolCalls.Add(ChatToolCall.CreateFunctionToolCall(toolCallUpdate.ToolCallId,
                        toolCallUpdate.FunctionName,
                        BinaryData.FromString(toolCallArgumentsStringBuilder.ToString())));
                    toolCallArgumentsStringBuilder.Clear();
                    currentToolName = toolCallUpdate.FunctionName;
                    currentToolCallId = toolCallUpdate.ToolCallId;
                }
            }

            if (!string.IsNullOrEmpty(currentToolName))
            {
                toolCalls.Add(ChatToolCall.CreateFunctionToolCall(currentToolCallId,
                    currentToolName,
                    BinaryData.FromString(toolCallArgumentsStringBuilder.ToString())));
            }

            if (toolCalls.Count == 0)
            {
                yield break;
            }

            messages = await ExecuteToolsAsync(messages, toolCalls);
        }
    }

    public ILlm UseTools(params object[] tools)
        => new OpenAILlm(model, apiKey, tools);

    private async Task<List<ChatMessage>> ExecuteToolsAsync(List<ChatMessage> messages,
        IReadOnlyList<ChatToolCall> toolCalls)
    {
        var toolTasks = new List<(Task<string> Task, ChatToolCall ToolCall)>();
        foreach (var toolCall in toolCalls)
        {
            if (!_toolMethods.TryGetValue(toolCall.FunctionName, out var toolMethod))
            {
                continue;
            }

            var parametersJson = JsonNode.Parse(toolCall.FunctionArguments)
                .AsObject();
            var toolTask = ToolExecutor.ExecuteToolAsync(toolMethod.Method, parametersJson, toolMethod.TargetObject);
            toolTasks.Add((toolTask, toolCall));
        }

        await Task.WhenAll(toolTasks.Select(t => t.Task));
        foreach (var (task, toolCall) in toolTasks)
        {
            var toolTaskResult = await task;
            messages.Add(new AssistantChatMessage(toolCalls));
            messages.Add(new ToolChatMessage(toolCall.Id, toolTaskResult));
        }

        return messages;
    }

    private List<ChatTool> GetTools()
    {
        if (tools?.Any() != true)
        {
            return [];
        }

        _toolMethods.Clear();
        foreach (var tool in tools)
        {
            foreach (var toolMethod in tool.GetType().GetToolMethods())
            {
                _toolMethods[toolMethod.Name] = (toolMethod, tool);
            }
        }

        var schemas = _toolMethods.Values
            .Select(m => (MethodName: m.Method.Name, Schema: SchemaGenerator.GenerateSchema(m.Method)))
            .ToList();

        return
            schemas
                .Select(m =>
                    ChatTool.CreateFunctionTool(m.MethodName, functionParameters: BinaryData.FromString(m.Schema)))
                .ToList();
    }
}