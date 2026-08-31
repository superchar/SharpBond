using SharpBond.Core;

namespace SharpBond.Examples.AzureServiceBus.Types;

public record StartWorkflow : Message;

public record RequestPoem : Message;

public record PoemResponse : Message;

public record RequestSummarization : Message;

public record SummarizationResponse : Message;

public record RequestReview : Message;

public record ReviewResponse(int Mark) : Message;

public record ResultResponse(string Poem) : Message;

public record AgentState(Guid SessionId, string Poem, string SummarizedPoem, bool ReviewPassed) : State(SessionId);