using SharpBond.Core;
using SharpBond.Core.Abstractions;

namespace SharpBond.Examples.AgentCollaboration;

public record StartWorkflow : Message;

public record RequestPoem : Message;

public record PoemResponse : Message;

public record RequestSummarization : Message;

public record SummarizationResponse : Message;

public record RequestReview : Message;

public record ReviewResponse(bool Approved) : Message;

public record ResultResponse(string Poem) : Message;

public record AgentState(Guid SessionId, string Poem, string SummarizedPoem, bool ReviewPassed) : State(SessionId);


public class PoemAgent : Agent, IHandles<AgentState, RequestPoem>
{
    private const string Poem = "The morning light arrives without a sound,\nA quiet thread of gold upon the floor,\nWhere shadows linger long before the day\nUnfolds its silver fingers at the door.\n\nThe world wakes slowly, breathing in the mist\nThat clings like silk to autumn’s yellow trees,\nAnd every leaf that falls becomes a note\nIn silent, unrecorded harmonies.\n\nWe gather moments, small and fleeting things—\nA sudden laugh, a warm and quiet room—\nAnd press them into pages of the mind\nTo keep the light when dark begins to loom.\n\nThe hours stretch out like ribbons in the wind,\nUntangling the knots of yesterday,\nAnd every step across the open field\nReminds us that the journey is the way.\n\nAcross the hill, the river finds its bend,\nIt never rushes where it needs to go;\nIt learns the patient language of the earth,\nThe deep, steady wisdom of the flow.\n\nAlong the bank, the tall wild grasses sway,\nCatching the last warm amber of the sun,\nWhile swallows trace their arcs across the blue,\nKnowing their daily work is almost done.\n\nThe distant hills grow soft in indigo,\nAs mist rises slow from sleeping streams,\nAnd in the quiet spaces between thoughts\nWe build our tiny castles made of dreams.\n\nAnd when twilight gathers in the dusk,\nThe stars begin their quiet, ancient rhyme,\nA thousand burning candles in the dark\nTo mark the slow, sweet passing of our time.\n\nSo let the evening rest upon your hands,\nAnd leave the heavy burdens of the day,\nFor in the stillness of the setting sun\nThe quiet beauty never fades away.\n\nThe night descends, a dark and velvet sky,\nWith secrets written in the silver dust,\nA gentle promise whispering through the trees\nThat even in the dark, the heart can trust.\n\nIt holds us close beneath its starry dome,\nUntil the dawn wakes up the world anew,\nAnd turns another page of quiet grace,\nTo bring the light once more to me and you.";
    
    public PoemAgent(ISessionStorage sessionStorage, IMessageRuntime messageRuntime) : base(sessionStorage, messageRuntime)
    {
    }

    public Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState agentState, RequestPoem message)
    {
        agentState = agentState with { Poem = Poem };
        return Task.FromResult((state: agentState, new List<Message> { new PoemResponse() }));
    }
}

public class SummarizationAgent : Agent, IHandles<AgentState, RequestSummarization>
{
    private const string SummarizedPoem =
        "The morning light arrives without a sound,\nUnfolding soft across the quiet floor,\nWhere shadows fade upon the misty ground\nAnd autumn waits beside the open door.\n\nWe gather moments, small and fleeting things—\nA river finding bend, a sudden breeze—\nAnd learn the quiet balance daylight brings,\nLike sunlight dancing through the golden trees.\n\nWhen twilight gathers in the velvet sky\nAnd stars light up their ancient, steady flame,\nWe rest in stillness as the hours pass by,\nKnowing the morning comes to call our name.";
    
    public SummarizationAgent(ISessionStorage sessionStorage, IMessageRuntime messageRuntime) : base(sessionStorage, messageRuntime)
    {
    }

    public Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState agentState, RequestSummarization message)
    {
        agentState = agentState with { SummarizedPoem = SummarizedPoem };
        
        return  Task.FromResult((state: agentState, new List<Message> { new SummarizationResponse() }));
    }
}

public class ReviewerAgent : Agent, IHandles<AgentState, RequestReview>
{
    public ReviewerAgent(ISessionStorage sessionStorage, IMessageRuntime messageRuntime) : base(sessionStorage, messageRuntime)
    {
    }

    public Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState agentState, RequestReview message)
    {
        return Task.FromResult((state: agentState, new List<Message> { new ReviewResponse(true) }));
    }
}

public class OrchestratorAgent : Agent, IHandles<AgentState, StartWorkflow>, IHandles<AgentState, PoemResponse>,
    IHandles<AgentState, SummarizationResponse>, IHandles<AgentState, ReviewResponse>
{
    public OrchestratorAgent(ISessionStorage sessionStorage, IMessageRuntime messageRuntime) : base(sessionStorage, messageRuntime)
    {
    }

    public Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState agentState, StartWorkflow message)
    {
        return Task.FromResult((state: agentState, new List<Message> { new RequestPoem() }));
    }

    public Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState agentState, PoemResponse message)
    {
        return Task.FromResult((state: agentState, new List<Message> { new RequestSummarization() }));
    }

    public Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState agentState, SummarizationResponse message)
    {
        return Task.FromResult((state: agentState, new List<Message> { new RequestReview() }));
    }

    public Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState agentState, ReviewResponse message)
        => Task.FromResult(message.Approved
            ? (state: agentState, [new ResultResponse(agentState.SummarizedPoem)])
            : (state: agentState, new List<Message>()));
}