using SharpBond.Core;
using SharpBond.Core.Abstractions;
using SharpBond.Core.Llm;
using SharpBond.Core.Tools;

namespace SharpBond.Examples.Tools;

public record StartMessage : Message;

public record ResultMessage(string Response) : Message;

public class GoogleAgent(IStateStorage stateStorage, IMessageRuntime messageRuntime, ILlm llm)
    : Agent(stateStorage, messageRuntime, llm), IHandles<Unit, StartMessage>
{
    public async Task<(Unit State, List<Message> Messages)> HandleAsync(Unit state, StartMessage message)
    {
        var result = await llm.UseTools(this)
            .GenerateAsync("Give me the first page that Google returns for query 'dogs'");

        return (state, [new ResultMessage(result)]);
    }

    [Tool]
    public string GetGoogleApiKey() => "a0df382b-a145-42a5-98ab-85342b4ca94e";

    [Tool]
    public string SearchInGoogle(string searchQuery, string apiKey) => $"The search result for query {searchQuery} is https://en.wikipedia.org/wiki/Dog";
}