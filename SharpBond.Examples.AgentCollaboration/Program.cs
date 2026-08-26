using SharpBond.Core.InMemory;
using SharpBond.Examples.AgentCollaboration;

Console.WriteLine("Hello, World!");

var sessionStorage = new InMemorySessionStorage();
var runtime = new InMemoryMessageRuntime(sessionStorage);

var poemAgent = new PoemAgent(sessionStorage, runtime);
var summarizationAgent = new SummarizationAgent(sessionStorage, runtime);
var reviewerAgent = new ReviewerAgent(sessionStorage, runtime);
var orchestratorAgent = new OrchestratorAgent(sessionStorage, runtime);

await runtime.SendAsync(new StartWorkflow(), new AgentState(Guid.NewGuid(), string.Empty, string.Empty, false));

Console.ReadLine();