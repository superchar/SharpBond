using SharpBond.Core.InMemory;
using SharpBond.Examples.AgentCollaboration;
using SharpBond.Integrations.OpenAI;

const string model = "gpt-5.1";
const string apiKey = "";
var stateStorage = new InMemoryStateStorage();
var runtime = new InMemoryMessageRuntime(stateStorage);
var llm = new OpenAILlm(model, apiKey);

var poemAgent = new PoemAgent(stateStorage, runtime, llm);
var summarizationAgent = new SummarizationAgent(stateStorage, runtime, llm);
var reviewerAgent = new ReviewerAgent(stateStorage, runtime, llm);
var orchestratorAgent = new OrchestratorAgent(stateStorage, runtime, llm);

var agentState = new AgentState(Guid.NewGuid(), string.Empty, string.Empty, false);

var result = await runtime.SendAndWaitAsync<StartWorkflow, ResultResponse>(new StartWorkflow(), agentState);

Console.WriteLine("------------------------------------------------------");
Console.WriteLine("Result poem: ");
Console.WriteLine(result.Poem);