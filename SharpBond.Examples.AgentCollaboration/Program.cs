using SharpBond.Core.InMemory;
using SharpBond.Examples.AgentCollaboration;
using SharpBond.Integrations.OpenAI;

const string model = "gpt-5.1";
const string apiKey = "";
var sessionStorage = new InMemorySessionStorage();
var runtime = new InMemoryMessageRuntime(sessionStorage);
var llm = new OpenAILlm(model, apiKey);

var poemAgent = new PoemAgent(sessionStorage, runtime, llm);
var summarizationAgent = new SummarizationAgent(sessionStorage, runtime, llm);
var reviewerAgent = new ReviewerAgent(sessionStorage, runtime, llm);
var orchestratorAgent = new OrchestratorAgent(sessionStorage, runtime, llm);

var agentState = new AgentState(Guid.NewGuid(), string.Empty, string.Empty, false);

var result = await runtime.SendAndWaitAsync<StartWorkflow, ResultResponse>(new StartWorkflow(), agentState);

Console.WriteLine("------------------------------------------------------");
Console.WriteLine("Result poem: ");
Console.WriteLine(result.Poem);