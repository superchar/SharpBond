using SharpBond.Core;
using SharpBond.Core.Abstractions;
using SharpBond.Core.StateHandling;

namespace SharpBond.Examples.AgentCollaboration;

public record StartWorkflow : Message;

public record RequestPoem : Message;

public record PoemResponse : Message;

public record RequestSummarization : Message;

public record SummarizationResponse : Message;

public record RequestReview : Message;

public record ReviewResponse(int Mark) : Message;

public record ResultResponse(string Poem) : Message;

public record AgentState(Guid SessionId, string Poem, string SummarizedPoem, bool ReviewPassed) : State(SessionId);

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