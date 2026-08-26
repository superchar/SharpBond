# SharpBond

<img width="1536" height="1024" alt="worflow" src="https://github.com/user-attachments/assets/ff9202dd-e0ce-40b5-9215-eec342f5bb27" />


SharpBond is an actor-inspired, message-driven agent framework for C#. It allows you to build collaborative, stateful workflows by defining specialized agents that communicate asynchronously through a centralized message runtime.

It uses `System.Threading.Channels` for safe concurrent message processing and abstracts session state management, making it ideal for multi-step AI tasks, data processing pipelines, or complex orchestration logic.

---

## Core Concepts

*   **Agent:** The base execution unit. Agents inherit from `Agent` and implement `IHandles<TState, TMessage>` for the specific messages they process. They run on isolated asynchronous channel workers.
*   **Message:** Strongly-typed records that agents send and receive.
*   **State:** Immutable session data tied to a `Guid SessionId`. It is automatically retrieved, updated, and saved via `ISessionStorage` during message processing.
*   **IMessageRuntime:** The central broker that routes messages to the correct agents and allows synchronous waiting for specific terminal messages.

---

## Quick Start

This example demonstrates an orchestration workflow where agents collaborate to generate, summarize, and review a poem.

### 1. Define Messages and State

First, define the state payload for your session and the messages that will drive your workflow.

```csharp
using SharpBond.Core;

// 1. Define State
public record AgentState(Guid SessionId, string Poem, string SummarizedPoem, bool ReviewPassed) : State(SessionId);

// 2. Define Messages
public record StartWorkflow : Message;
public record RequestPoem : Message;
public record PoemResponse : Message;
public record ResultResponse(string Poem) : Message;
```

### 2. Create Agents

Agents handle specific messages and return an updated state along with any new messages to queue.

**Worker Agent:**
```csharp
public class PoemAgent : Agent, IHandles<AgentState, RequestPoem>
{
    public PoemAgent(ISessionStorage sessionStorage, IMessageRuntime messageRuntime) 
        : base(sessionStorage, messageRuntime) { }

    public Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState state, RequestPoem message)
    {
        var newState = state with { Poem = "The morning light arrives without a sound..." };
        
        // Return the updated state and trigger the next step
        return Task.FromResult((newState, new List<Message> { new PoemResponse() }));
    }
}
```

**Orchestrator Agent:**
An orchestrator listens to responses and routes the workflow to the next agent.
```csharp
public class OrchestratorAgent : Agent, 
    IHandles<AgentState, StartWorkflow>, 
    IHandles<AgentState, PoemResponse>
{
    public OrchestratorAgent(ISessionStorage sessionStorage, IMessageRuntime messageRuntime) 
        : base(sessionStorage, messageRuntime) { }

    public Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState state, StartWorkflow message)
    {
        return Task.FromResult((state, new List<Message> { new RequestPoem() }));
    }

    public Task<(AgentState State, List<Message> Messages)> HandleAsync(AgentState state, PoemResponse message)
    {
        // Finish the workflow by emitting the final result
        return Task.FromResult((state, new List<Message> { new ResultResponse(state.Poem) }));
    }
}
```

### 3. Initialize and Run

Use the in-memory implementations to bootstrap the runtime, register your agents, and start the workflow. 

```csharp
using SharpBond.Core.InMemory;

// Initialize infrastructure
var sessionStorage = new InMemorySessionStorage();
var runtime = new InMemoryMessageRuntime(sessionStorage);

// Register agents (Runtime auto-registers them in the base constructor)
var poemAgent = new PoemAgent(sessionStorage, runtime);
var orchestratorAgent = new OrchestratorAgent(sessionStorage, runtime);

// Initialize session state
var sessionId = Guid.NewGuid();
var initialState = new AgentState(sessionId, string.Empty, string.Empty, false);

// Send the initial message and wait for the ResultResponse
var result = await runtime.SendAndWaitAsync<StartWorkflow, ResultResponse>(
    new StartWorkflow(), 
    initialState
);

Console.WriteLine($"Workflow finished! Result: {result.Poem}");
```
