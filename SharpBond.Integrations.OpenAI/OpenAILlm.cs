using OpenAI.Chat;
using SharpBond.Core.Abstractions;

namespace SharpBond.Integrations.OpenAI;

public class OpenAILlm(string model, string apiKey) : ILlm
{
    private readonly ChatClient _chatClient = new(model, apiKey);

    public async Task<string> GenerateAsync(string prompt)
    {
        var result = await _chatClient.CompleteChatAsync(new UserChatMessage(prompt));

        return result.Value.Content[0].Text;
    }

    public async IAsyncEnumerable<string> GenerateStreamingAsync(string prompt, CancellationToken cancellationToken)
    {
        var result = _chatClient.CompleteChatStreamingAsync(new UserChatMessage(prompt));
        
        await foreach(var token in result)
        {
            yield return token.ContentUpdate[0].Text;
        }
    }
}