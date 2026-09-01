using System.Text.Json.Nodes;

namespace SharpBond.Core.Llm.Model;

public record ToolCallMessage(string Id, string ToolName, JsonObject Parameters) : LlmMessage;