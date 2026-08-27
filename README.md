# SharpBond

<img width="1536" height="1024" alt="ChatGPT Image Aug 26, 2026, 06_30_38 PM" src="https://github.com/user-attachments/assets/bd14c6bc-9603-4af6-a30a-eeb102ae591c" />

# SharpBond

SharpBond is an actor-inspired, message-driven agent framework for C#. It allows you to build collaborative, stateful workflows by defining specialized agents that communicate asynchronously through a centralized message runtime.

It uses `System.Threading.Channels` for safe concurrent message processing and abstracts session state management, making it ideal for multi-step AI tasks, data processing pipelines, or complex orchestration logic.

---

## Core Concepts

* **Agent:** The base execution unit. Agents inherit from `Agent` and implement `IHandles<TState, TMessage>` for the specific messages they process. They can leverage built-in abstractions like `ILlm` for AI tasks.
* **Message:** Strongly-typed records that agents send and receive to trigger state transitions or worker actions.
* **State:** Immutable session data tied to a `Guid SessionId`. It is automatically retrieved, updated, and saved via `ISessionStorage` during message processing. For stateless workflows, `Unit` can be used.
* **IMessageRuntime:** The central broker that routes messages to registered agents and allows synchronous waiting for terminal messages.
* **ILlm:** An abstraction layer for LLM integrations (e.g., OpenAI) allowing agents to generate dynamic responses.
* **Tools:** Methods decorated with the `[Tool]` attribute can be exposed to the LLM. They can accept any object, return any object, and be asynchronous, enabling LLMs to interact with external systems, APIs, or local logic during generation.

---

## Quick Start: Multi-Agent Collaboration

This example demonstrates an automated multi-agent collaboration loop where specialized agents generate, summarize, and review a poem using an LLM until quality conditions are met.

### 1. Define Messages and State

Define the shared session state and the domain messages that drive the orchestration workflow.

```csharp
using SharpBond.Core;

namespace SharpBond.Examples.AgentCollaboration;

// 1. Define Session State
public record AgentState(Guid SessionId, string Poem, string SummarizedPoem, bool ReviewPassed) : State(SessionId);

// 2. Define Workflow Messages
public record StartWorkflow : Message;
public record RequestPoem : Message;
public record PoemResponse : Message;
public record RequestSummarization : Message;
public record SummarizationResponse : Message;
public record RequestReview : Message;
public record ReviewResponse(int Mark) : Message;
public record ResultResponse(string Poem) : Message;
```

### 2. Create Agents

Agents implement message handlers to perform specific logic, update the session state, and produce subsequent messages.

**Worker Agents:**

```csharp
using SharpBond.Core;
using SharpBond.Core.Abstractions;

public class PoemAgent(ISessionStorage sessionStorage, IMessageRuntime messageRuntime, ILlm llm)
    : Agent(sessionStorage, messageRuntime, llm), 
      IHandles<AgentState, RequestPoem>
{
    private const string Prompt = "Generate 5 verse poem";

    public async Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState agentState, RequestPoem message)
    {
        var poem = await llm.GenerateAsync(Prompt);
        Console.WriteLine($"Agent {nameof(PoemAgent)}:\nGenerated poem:\n{poem}\n---");

        agentState = agentState with { Poem = poem };
        return (agentState, [new PoemResponse()]);
    }
}

public class SummarizationAgent(ISessionStorage sessionStorage, IMessageRuntime messageRuntime, ILlm llm)
    : Agent(sessionStorage, messageRuntime, llm), 
      IHandles<AgentState, RequestSummarization>
{
    private const string Prompt = "Summarize 5 verse poem to 3 verse. Original poem : {0}";

    public async Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState agentState, RequestSummarization message)
    {
        var formattedPrompt = string.Format(Prompt, agentState.Poem);
        var summarizedPoem = await llm.GenerateAsync(formattedPrompt);
        Console.WriteLine($"Agent {nameof(SummarizationAgent)}:\nSummarized poem:\n{summarizedPoem}\n---");

        agentState = agentState with { SummarizedPoem = summarizedPoem };
        return (agentState, [new SummarizationResponse()]);
    }
}

public class ReviewerAgent(ISessionStorage sessionStorage, IMessageRuntime messageRuntime, ILlm llm)
    : Agent(sessionStorage, messageRuntime, llm), 
      IHandles<AgentState, RequestReview>
{
    private const string Prompt = "Given a poem and a summarized version of the poem, give it a mark from 1 to 100. Return mark only. Poem: {0}, Summarized Poem: {1}";

    public async Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState agentState, RequestReview message)
    {
        var formattedPrompt = string.Format(Prompt, agentState.Poem, agentState.SummarizedPoem);
        var result = await llm.GenerateAsync(formattedPrompt);
        return (agentState, [new ReviewResponse(int.Parse(result.Trim()))]);
    }
}
```

**Orchestrator Agent:**

The orchestrator controls the execution flow, routing state between workers and handling feedback loops based on evaluation scores.

```csharp
public class OrchestratorAgent(ISessionStorage sessionStorage, IMessageRuntime messageRuntime, ILlm llm)
    : Agent(sessionStorage, messageRuntime, llm),
      IHandles<AgentState, StartWorkflow>,
      IHandles<AgentState, PoemResponse>,
      IHandles<AgentState, SummarizationResponse>,
      IHandles<AgentState, ReviewResponse>
{
    private const int MinMark = 90;

    public Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState agentState, StartWorkflow message)
    {
        return Task.FromResult((agentState, new List<Message> { new RequestPoem() }));
    }

    public Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState agentState, PoemResponse message)
    {
        return Task.FromResult((agentState, new List<Message> { new RequestSummarization() }));
    }

    public Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState agentState, SummarizationResponse message)
    {
        return Task.FromResult((agentState, new List<Message> { new RequestReview() }));
    }

    public Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState agentState, ReviewResponse message)
    {
        Console.WriteLine($"Review score: {message.Mark}\n---");

        if (message.Mark >= MinMark)
        {
            return Task.FromResult((agentState, new List<Message> { new ResultResponse(agentState.SummarizedPoem) }));
        }

        Console.WriteLine("Score is below threshold. Retrying generation flow...\n---");
        return Task.FromResult((agentState, new List<Message> { new RequestPoem() }));
    }
}
```

### 3. Initialize and Run

Wire up infrastructure components, register your agents, and dispatch the initial message to trigger the pipeline.

```csharp
using SharpBond.Core.InMemory;
using SharpBond.Examples.AgentCollaboration;
using SharpBond.Integrations.OpenAI;

// 1. Setup infrastructure and LLM integration
const string model = "gpt-5.1";
const string apiKey = "YOUR_OPENAI_API_KEY";

var sessionStorage = new InMemorySessionStorage();
var runtime = new InMemoryMessageRuntime(sessionStorage);
var llm = new OpenAILlm(model, apiKey);

// 2. Instantiate agents (Agents automatically register with runtime on instantiation)
var poemAgent = new PoemAgent(sessionStorage, runtime, llm);
var summarizationAgent = new SummarizationAgent(sessionStorage, runtime, llm);
var reviewerAgent = new ReviewerAgent(sessionStorage, runtime, llm);
var orchestratorAgent = new OrchestratorAgent(sessionStorage, runtime, llm);

// 3. Initialize state and run workflow synchronously to result
var agentState = new AgentState(Guid.NewGuid(), string.Empty, string.Empty, false);
var result = await runtime.SendAndWaitAsync<StartWorkflow, ResultResponse>(new StartWorkflow(), agentState);

Console.WriteLine("Final Result Poem:");
Console.WriteLine(result.Poem);
```

---

## Tool Calling (Function Calling)

SharpBond agents can expose tools to the LLM during generation. Tools are simply methods decorated with the `[Tool]` attribute. They support asynchronous execution as well as arbitrary parameter and return types.

### 1. Define an Agent with Tools

To expose tools to the LLM, call `llm.UseTools(this)` before invoking `GenerateAsync`. For stateless interactions, use `Unit`.

```csharp
using SharpBond.Core;
using SharpBond.Core.Abstractions;
using SharpBond.Core.Tools;

namespace SharpBond.Examples.Tools;

public record StartMessage : Message;
public record ResultMessage(string Response) : Message;

public class GoogleAgent(ISessionStorage sessionStorage, IMessageRuntime messageRuntime, ILlm llm)
    : Agent(sessionStorage, messageRuntime, llm), 
      IHandles<Unit, StartMessage>
{
    public async Task<(Unit State, List<Message> Messages)> HandleAsync(Unit state, StartMessage message)
    {
        // 1. Bind agent tools to the LLM context and generate
        var result = await llm.UseTools(this)
            .GenerateAsync("Give me the first page that Google returns for query 'dogs'");

        return (state, [new ResultMessage(result)]);
    }

    // 2. Decorate methods with [Tool] to expose them to the LLM
    [Tool]
    public string GetGoogleApiKey() => "a0df382b-a145-42a5-98ab-85342b4ca94e";

    [Tool]
    public string SearchInGoogle(string searchQuery, string apiKey) 
        => $"The search result for query {searchQuery} is [https://en.wikipedia.org/wiki/Dog](https://en.wikipedia.org/wiki/Dog)";
}
```

### 2. Run the Tool Calling Workflow

```csharp
using SharpBond.Core;
using SharpBond.Core.InMemory;
using SharpBond.Examples.Tools;
using SharpBond.Integrations.OpenAI;

const string model = "gpt-5.1";
const string apiKey = "YOUR_OPENAI_API_KEY";

var sessionStorage = new InMemorySessionStorage();
var runtime = new InMemoryMessageRuntime(sessionStorage);
var llm = new OpenAILlm(model, apiKey);

var googleAgent = new GoogleAgent(sessionStorage, runtime, llm);

// Trigger workflow with Unit.Value for stateless execution
var result = await runtime.SendAndWaitAsync<StartMessage, ResultMessage>(new StartMessage(), Unit.Value);

Console.WriteLine($"The search result is: {result.Response}");
```
