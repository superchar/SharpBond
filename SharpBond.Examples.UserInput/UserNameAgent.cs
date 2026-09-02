using SharpBond.Core;
using SharpBond.Core.Abstractions;
using SharpBond.Core.Llm;
using SharpBond.Core.Llm.Model;
using SharpBond.Core.StateHandling;
using SharpBond.Core.Tools;

namespace SharpBond.Examples.UserInput;

public record UserNameAgentState(Guid SessionId, List<LlmMessage> Messages) : State(SessionId)
{
    public bool UserInputRequired { get; set; }

    public string UserInputMessage { get; set; }
}

public record AskUserName : Message;

public record InputRequiredResponse(string Message) : Message;

public record UserInputProvided(string UserInput) : Message;

public record InputProvidedResponse(string Message) : Message;

public class UserNameAgent(IStateStorage stateStorage, IMessageRuntime messageRuntime, ILlm llm)
    : Agent(stateStorage, messageRuntime, llm), IHandles<UserNameAgentState, AskUserName>,
        IHandles<UserNameAgentState, UserInputProvided>
{
    public async Task<(UserNameAgentState State, List<Message> Messages)> HandleAsync(UserNameAgentState state, AskUserName message)
    {
        var messages = new List<LlmMessage> { new UserMessage("Find out user name") };
        messages = await llm.UseTools(this).GenerateAsync(messages, state);
        state = state with { Messages = messages };

        return (state, []);
    }

    public async Task<(UserNameAgentState State, List<Message> Messages)> HandleAsync(UserNameAgentState state, UserInputProvided message)
    {
        state.Messages.Add(new UserMessage(message.UserInput));
        var messages = await llm.GenerateAsync(state.Messages);
        state = state with { Messages = messages };
        var assistanceMessage = (state.Messages.Last() as AssistantMessage)?.Message ?? string.Empty;

        return (state, [new InputProvidedResponse(assistanceMessage)]);
    }
    
    [Tool(Description = "User this tool to ask user his name")]
    public async Task<string> AskUserNameAsync(string askMessage, UserNameAgentState state)
    {
        state.UserInputRequired = true;
        state.UserInputMessage = askMessage;
        await messageRuntime.SendAsync(new InputRequiredResponse(askMessage), state.SessionId);

        return "User input requested. Wait for the response";
    }
}
    
  