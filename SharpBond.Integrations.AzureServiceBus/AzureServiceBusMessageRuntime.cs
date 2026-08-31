using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using SharpBond.Core;
using SharpBond.Core.Abstractions;
using SharpBond.Core.Helpers;
using SharpBond.Core.Serialization;

namespace SharpBond.Integrations.AzureServiceBus;

public class AzureServiceBusMessageRuntime(string connectionString, IStateStorage stateStorage)
    : IMessageRuntime, IAsyncDisposable
{
    private readonly ServiceBusClient _client = new(connectionString);
    private readonly ServiceBusAdministrationClient _administrationClient = new(connectionString);

    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();
    private readonly ConcurrentDictionary<string, ServiceBusProcessor> _processors = new();
    private readonly ConcurrentDictionary<Type, ConcurrentBag<Agent>> _agentRegistry = new();
    private readonly ConcurrentDictionary<(Type, Guid SessionId), TaskCompletionSource<object>> _waiters = new();

    public async Task<TWaitMessage> SendAndWaitAsync<TMessage, TWaitMessage>(TMessage message, State state)
        where TMessage : Message where TWaitMessage : Message
    {
        await stateStorage.PutAsync(state.SessionId, state);
        var waitTask = WaitAsync<TWaitMessage>(state.SessionId);
        await SendAsync(message, state.SessionId);

        return await waitTask;
    }

    public async Task SendAsync<TMessage>(TMessage message, Guid sessionId) where TMessage : Message
    {
        var messageType = message.GetType();
        var queueName = messageType.FullName 
            ?? throw new InvalidOperationException($"Type {messageType} must have a valid FullName.");

        var sender = _senders.GetOrAdd(queueName, name => _client.CreateSender(name));
        
        var jsonPayload = PolymorphicSerialization.Serialize(message);
        var serviceBusMessage = new ServiceBusMessage(jsonPayload)
        {
            ApplicationProperties =
            {
                ["SessionId"] = sessionId.ToString()
            }
        };

        await sender.SendMessageAsync(serviceBusMessage);
    }

    public async Task RegisterAsync<TAgent>(TAgent agent) where TAgent : Agent
    {
        var handledMessages = agent
            .GetType()
            .GetHandledInterfaces()
            .Select(i => i.GetGenericArguments()[1])
            .ToList();

        foreach (var handledMessage in handledMessages)
        {
            var queueName = handledMessage.FullName 
                ?? throw new InvalidOperationException($"Type {handledMessage} must have a valid FullName.");

            _agentRegistry.GetOrAdd(handledMessage, _ => []).Add(agent);
            
            if (_processors.ContainsKey(queueName))
            {
                continue;
            }
            
            if (!await _administrationClient.QueueExistsAsync(queueName))
            {
                await _administrationClient.CreateQueueAsync(queueName);
            }

            var processor = _client.CreateProcessor(queueName);

            processor.ProcessMessageAsync += args =>
            {
                try
                {
                    if (!args.Message.ApplicationProperties.TryGetValue("SessionId", out var rawSessionId) ||
                        !Guid.TryParse(rawSessionId?.ToString(), out var sessionId))
                    {
                        return Task.CompletedTask;
                    }
                    
                    var deserializedMessage = PolymorphicSerialization.Deserialize<object>(args.Message.Body.ToString());
                    if (deserializedMessage is Message messageObj)
                    {
                        ReceiveMessage(handledMessage, messageObj, sessionId);
                    }

                    return Task.CompletedTask;
                }
                catch (Exception exception)
                {
                    return Task.FromException(exception);
                }
            };

            if (_processors.TryAdd(queueName, processor))
            {
                await processor.StartProcessingAsync();
            }
        }
    }

    private void ReceiveMessage(Type messageType, object messageBody, Guid sessionId)
    {
        if (_waiters.TryRemove((messageType, sessionId), out var taskCompletionSource))
        {
            taskCompletionSource.SetResult(messageBody);
        }

        if (!_agentRegistry.TryGetValue(messageType, out var agents))
        {
            return;
        }
        
        foreach (var agent in agents)
        {
            agent.QueueMessage(messageBody, sessionId);
        }
    }

    private async Task<TMessage> WaitAsync<TMessage>(Guid sessionId) where TMessage : Message
    {
        var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        _waiters[(typeof(TMessage), sessionId)] = taskCompletionSource;
        
        var result = await taskCompletionSource.Task;
        return (TMessage)result;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var processor in _processors.Values)
        {
            await processor.StopProcessingAsync();
            await processor.DisposeAsync();
        }

        foreach (var sender in _senders.Values)
        {
            await sender.DisposeAsync();
        }

        await _client.DisposeAsync();
    }
}