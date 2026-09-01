namespace SharpBond.Core.Llm.Model;

public record ToolResponseMessage(string Id, string Response) : LlmMessage;