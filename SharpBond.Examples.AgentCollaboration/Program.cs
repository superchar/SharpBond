using SharpBond.Core.InMemory;
using SharpBond.Examples.AgentCollaboration;

var sessionStorage = new InMemorySessionStorage();
var runtime = new InMemoryMessageRuntime(sessionStorage);

var poemAgent = new PoemAgent(sessionStorage, runtime);
var summarizationAgent = new SummarizationAgent(sessionStorage, runtime);
var reviewerAgent = new ReviewerAgent(sessionStorage, runtime);
var orchestratorAgent = new OrchestratorAgent(sessionStorage, runtime);

var agentState = new AgentState(Guid.NewGuid(), string.Empty, string.Empty, false);

var result = await runtime.SendAndWaitAsync<StartWorkflow, ResultResponse>(new StartWorkflow(), agentState);

Console.WriteLine(result.Poem);