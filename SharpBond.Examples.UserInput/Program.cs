using SharpBond.Core.InMemory;
using SharpBond.Examples.UserInput;
using SharpBond.Integrations.OpenAI;

const string model = "gpt-5.1";
const string apiKey = "";
var stateStorage = new InMemoryStateStorage();
var runtime = new InMemoryMessageRuntime(stateStorage);
var llm = new OpenAILlm(model, apiKey);

var userNameAgent = new UserNameAgent(stateStorage, runtime, llm);

var userAgentState = new UserNameAgentState(Guid.NewGuid(), []);

var message = await runtime.SendAndWaitAsync<AskUserName, InputRequiredResponse>(new AskUserName(), userAgentState);
Console.Write($"{message.Message} : ");
var userInput = Console.ReadLine();
var inputProvidedResponse = await runtime.SendAndWaitAsync<UserInputProvided, InputProvidedResponse>(new UserInputProvided(userInput), userAgentState);
Console.WriteLine(inputProvidedResponse.Message);
