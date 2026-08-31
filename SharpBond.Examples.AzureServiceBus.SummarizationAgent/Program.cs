using SharpBond.Core;
using SharpBond.Core.Abstractions;
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

var agent = new SummarizationAgent(stateStorage, runtime, llm);

Console.WriteLine($"{nameof(SummarizationAgent)} started press any key to stop");
Console.ReadKey();

public class SummarizationAgent(IStateStorage stateStorage, IMessageRuntime messageRuntime, ILlm llm)
    : Agent(stateStorage,
        messageRuntime,
        llm), IHandles<AgentState, RequestSummarization>
{
    private const string Prompt = "Summarize 5 verse poem to 3 verse. Original poem : {0}";

    public async Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState agentState,
        RequestSummarization message)
    {
        var formattedPrompt = string.Format(Prompt, agentState.Poem);
        var summarizedPoem = await llm.GenerateAsync(formattedPrompt);
        Console.WriteLine($"Agent {nameof(SummarizationAgent)} : {Environment.NewLine} Summarized poem : {summarizedPoem}");
        Console.WriteLine("------------------------------------------------------");
        agentState = agentState with { SummarizedPoem = summarizedPoem };

        return (agentState, [new SummarizationResponse()]);
    }
}