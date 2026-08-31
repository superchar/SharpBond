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

var agent = new OrchestratorAgent(stateStorage, runtime, llm);

Console.WriteLine($"{nameof(OrchestratorAgent)} started press any key to stop");
Console.ReadKey();

public class OrchestratorAgent(IStateStorage stateStorage, IMessageRuntime messageRuntime, ILlm llm)
    : Agent(stateStorage,
            messageRuntime,
            llm), IHandles<AgentState, StartWorkflow>, IHandles<AgentState, PoemResponse>,
        IHandles<AgentState, SummarizationResponse>, IHandles<AgentState, ReviewResponse>
{
    private const int MinMark = 90;
    
    public Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState agentState, StartWorkflow message)
    {
        return Task.FromResult((state: agentState, new List<Message> { new RequestPoem() }));
    }

    public Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState agentState, PoemResponse message)
    {
        return Task.FromResult((state: agentState, new List<Message> { new RequestSummarization() }));
    }

    public Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState agentState,
        SummarizationResponse message)
    {
        return Task.FromResult((state: agentState, new List<Message> { new RequestReview() }));
    }

    public Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState agentState, ReviewResponse message)
    {
        Console.WriteLine($"The review mark is : {message.Mark}");
        Console.WriteLine("------------------------------------------------------");
        if (message.Mark >= MinMark)
        {
            return Task.FromResult((agentState, new List<Message> { new ResultResponse(agentState.SummarizedPoem) }));
        }

        Console.WriteLine("The reviewer mark is not sufficient. Starting over");
        Console.WriteLine("------------------------------------------------------");
        return Task.FromResult((agentState, new List<Message> { new RequestPoem() }));
    }
}