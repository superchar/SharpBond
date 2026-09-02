using SharpBond.Core.Llm.Model;
using SharpBond.Core.StateHandling;

namespace SharpBond.Core.Llm;

public interface ILlm
{
    Task<string> GenerateAsync(string prompt, State? state = null);

    Task<List<LlmMessage>> GenerateAsync(List<LlmMessage> messages, State? state = null);

    IAsyncEnumerable<string> GenerateStreamingAsync(string prompt, CancellationToken cancellationToken,
        State? state = null);

    IAsyncEnumerable<string> GenerateStreamingAsync(List<LlmMessage> messages, CancellationToken cancellationToken,
        State? state = null);

    ILlm UseTools(params object[] tools);
}