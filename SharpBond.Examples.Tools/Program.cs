using SharpBond.Core;
using SharpBond.Core.InMemory;
using SharpBond.Examples.Tools;
using SharpBond.Integrations.OpenAI;

const string model = "gpt-5.1";
const string apiKey = "";
var sessionStorage = new InMemorySessionStorage();
var runtime = new InMemoryMessageRuntime(sessionStorage);
var llm = new OpenAILlm(model, apiKey);

var googleAgent = new GoogleAgent(sessionStorage, runtime, llm);

var result = await runtime.SendAndWaitAsync<StartMessage, ResultMessage>(new StartMessage(), Unit.Value);

Console.WriteLine($"The search result is {result.Response}");