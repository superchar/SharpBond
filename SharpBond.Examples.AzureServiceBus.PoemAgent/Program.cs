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

var agent = new PoemAgent(stateStorage, runtime, llm);

Console.WriteLine($"{nameof(PoemAgent)} started press any key to stop");
Console.ReadKey();

public class PoemAgent(IStateStorage stateStorage, IMessageRuntime messageRuntime, ILlm llm)
    : Agent(stateStorage,
        messageRuntime,
        llm), IHandles<AgentState, RequestPoem>
{
    private const string Prompt = "Generate 5 verse poem";
    
    public async Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState agentState, RequestPoem message)
    {
        var poem = await llm.GenerateAsync(Prompt);
        Console.WriteLine($"Agent {nameof(PoemAgent)}: {Environment.NewLine} Generated poem : {poem}");
        Console.WriteLine("------------------------------------------------------");
        agentState = agentState with { Poem = poem };
        return (agentState, [new PoemResponse()]);
    }
}