# SharpBond

<img width="1536" height="1024" alt="ChatGPT Image Aug 26, 2026, 06_30_38 PM" src="https://github.com/user-attachments/assets/bd14c6bc-9603-4af6-a30a-eeb102ae591c" />

SharpBond is an actor-inspired, message-driven agent framework for C#. It allows you to build collaborative, stateful workflows by defining specialized agents that communicate asynchronously through a centralized message runtime.

It uses `System.Threading.Channels` for safe concurrent message processing and abstracts session state management, making it ideal for multi-step AI tasks, data processing pipelines, or complex orchestration logic.

---

## Core Concepts

* **Agent:** The base execution unit. Agents inherit from `Agent` and implement `IHandles<TState, TMessage>` for the specific messages they process. They can leverage built-in abstractions like `ILlm` for AI tasks.
* **Message:** Strongly-typed records that agents send and receive to trigger state transitions or worker actions.
* **State:** Immutable session data tied to a `Guid SessionId`. It is automatically retrieved, updated, and saved via `ISessionStorage` during message processing. For stateless workflows, `Unit` can be used.
* **IMessageRuntime:** The central broker that routes messages to registered agents and allows synchronous waiting for terminal messages.
* **ILlm:** An abstraction layer for LLM integrations (e.g., OpenAI) allowing agents to generate dynamic responses.
* **Tools:** Methods decorated with the `[Tool]` attribute can be exposed to the LLM. Tools can accept any object, return any object, and execute asynchronously, enabling LLMs to interact with external APIs, databases, or local services.

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
