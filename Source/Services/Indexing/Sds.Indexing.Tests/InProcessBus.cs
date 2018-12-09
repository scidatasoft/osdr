using CQRSlite.Bus;
using CQRSlite.Commands;
using CQRSlite.Events;
using CQRSlite.Messages;
using Sds.CqrsLite.Saga;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sds.CqrsLite
{
    public class InProcessBus : ICommandSender, IEventPublisher, IHandlerRegistrar, ISagaRegistrar
    {
        private readonly Dictionary<Type, List<Func<IMessage, CancellationToken, Task>>> _routes = new Dictionary<Type, List<Func<IMessage, CancellationToken, Task>>>();

        public void RegisterHandler<T>(Func<T, CancellationToken, Task> handler) where T : class, IMessage
        {
            if (!_routes.TryGetValue(typeof(T), out var handlers))
            {
                handlers = new List<Func<IMessage, CancellationToken, Task>>();
                _routes.Add(typeof(T), handlers);
            }
            handlers.Add((message, token) => handler((T)message, token));
        }

        public Task Send<T>(T command, CancellationToken cancellationToken = default(CancellationToken)) where T : class, ICommand
        {
            if (!_routes.TryGetValue(command.GetType(), out var handlers))
                throw new InvalidOperationException($"No handler registered for command {typeof(T)}");
            if (handlers.Count != 1)
                throw new InvalidOperationException($"Cannot send command {typeof(T)} to more than one handler");
            return handlers[0](command, cancellationToken);
        }

        public Task Publish<T>(T @event, CancellationToken cancellationToken = default(CancellationToken)) where T : class, IEvent
        {
            if (!_routes.TryGetValue(@event.GetType(), out var handlers))
                return Task.CompletedTask;
            return Task.WhenAll(handlers.Select(handler => handler(@event, cancellationToken)));
        }

        public Task RegisterSagaHandler<TSaga, TState, TMessage>(Func<TMessage, CancellationToken, Task> handler)
            where TSaga : ISaga<TState>
            where TState : IState
            where TMessage : IMessage
        {
            if (!_routes.TryGetValue(typeof(TMessage), out var handlers))
            {
                handlers = new List<Func<IMessage, CancellationToken, Task>>();
                _routes.Add(typeof(TMessage), handlers);
            }
            handlers.Add((message, token) => handler((TMessage)message, token));

            return Task.CompletedTask;
        }
    }
}
