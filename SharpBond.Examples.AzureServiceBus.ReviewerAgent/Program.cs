using SharpBond.Core;
using SharpBond.Core.Abstractions;
using SharpBond.Core.Llm;
using SharpBond.Examples.AzureServiceBus.Types;
using SharpBond.Integrations.AzureServiceBus;
using SharpBond.Integrations.OpenAI;
using SharpBond.Integrations.Redis;

const string model = "gpt-5.1";
const string openAiApiKey = "";
const string redisConnectionString = "localhost:6379";
const string azureServiceBusConnectionString = "";
var stateStorage = new RedisStateStorage(redisConnectionString);
var runtime = new AzureServiceBusMessageRuntime(azureServiceBusConnectionString, stateStorage);
var llm = new OpenAILlm(model, openAiApiKey);

var agent = new ReviewerAgent(stateStorage, runtime, llm);

Console.WriteLine($"{nameof(ReviewerAgent)} started press any key to stop");
Console.ReadKey();


public class ReviewerAgent(IStateStorage stateStorage, IMessageRuntime messageRuntime, ILlm llm)
    : Agent(stateStorage,
        messageRuntime,
        llm), IHandles<AgentState, RequestReview>
{
    private const string Prompt =
        "Given a poem a summized version of the poem give it mark from 1 to 100. Return mark only. Poem : {0}, Summarized Poem : {1}";
    
    public async Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState agentState, RequestReview message)
    {
        var formattedPrompt = string.Format(Prompt, agentState.Poem, agentState.SummarizedPoem);
        var result = await llm.GenerateAsync(formattedPrompt);
        return (agentState, [new ReviewResponse(int.Parse(result))]);
    }
}
