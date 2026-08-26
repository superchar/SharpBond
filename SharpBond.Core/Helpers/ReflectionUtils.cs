using System.Collections;
using SharpBond.Core.Abstractions;

namespace SharpBond.Core.Helpers;

public static class ReflectionUtils
{
    extension(Type type)
    {
        public List<Type> GetHandledInterfaces()
            => type.GetInterfaces()
                .Where(i => i.GetGenericTypeDefinition() == typeof(IHandles<,>))
                .ToList();

        public async Task<(State State, IEnumerable Messages)> CallHandleMethodAsync(object target, object message, State state)
        {
            var handleInterface = type
                .GetHandledInterfaces()
                .Single(i =>
                {
                    var args = i.GetGenericArguments();

                    return args[0] == state.GetType() && args[1] == message.GetType();
                });
        
            var handleMethod = handleInterface.GetMethods().Single(m => m.Name == nameof(IHandles<,>.HandleAsync));
            var task = (Task)handleMethod.Invoke(target, [state, message]);
            
            await task;
            
            var resultProperty = task.GetType().GetProperty("Result");
            var tupleResult = resultProperty.GetValue(task);

            var tupleType = tupleResult.GetType();
            var newState = (State)tupleType.GetField("Item1").GetValue(tupleResult);
            var newMessages = (IEnumerable)tupleType.GetField("Item2").GetValue(tupleResult);
        
            return (newState, newMessages);
        }
    }
}