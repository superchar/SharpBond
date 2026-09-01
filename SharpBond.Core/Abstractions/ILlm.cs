using SharpBond.Core.Llm.Model;

namespace SharpBond.Core.Abstractions;

public interface ILlm
{
    Task<string> GenerateAsync(string prompt);
    
    Task<List<LlmMessage>> GenerateAsync(List<LlmMessage> messages);
    
    IAsyncEnumerable<string> GenerateStreamingAsync(string prompt, CancellationToken cancellationToken);

    IAsyncEnumerable<string> GenerateStreamingAsync(List<LlmMessage> messages, CancellationToken cancellationToken);
    
    ILlm UseTools(params object[] tools);
}