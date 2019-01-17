using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DistributedDojo
{
    public class InMemoryBus : IMessagingEndpoint, IMessageHandlerContext
    {
        private readonly List<Subscription> subscriptions = new List<Subscription>();
        private readonly Queue<object> messages = new Queue<object>();

        private class Subscription
        {
            public Subscription(Type type, object factory)
            {
                this.Type = type;
                this.Factory = factory;
            }
            
            public Type Type { get; }
            public object Factory { get; }
        }
        
        public Task Send(object message)
        {
            bool NotACommand() => !message.GetType().Name.EndsWith("Command");
            
            if (NotACommand())
            {
                throw new ArgumentException($"Only commands allowed. `{message.GetType()}` is not a command.");
            }
            
            this.messages.Enqueue(message);
            return Task.CompletedTask;
        }

        public Task Publish(object message)
        {
            bool NotAnEvent() => !message.GetType().Name.EndsWith("Event");

            if (NotAnEvent())
            {
                throw new ArgumentException($"Only events allowed. `{message.GetType()}` is not an event.");
            }
            
            this.messages.Enqueue(message);
            return Task.CompletedTask;
        }

        private Task HandleMessage(object message)
        {
            var matchingSubscriptions = this.subscriptions.Where(_ => _.Type == message.GetType());

            var tasks = new List<Task>();
            
            foreach (Subscription subscription in matchingSubscriptions)
            {
                var createMethod = subscription.Factory.GetType().GetMethod("Create");
                var handler = createMethod.Invoke(subscription.Factory, new object[0]);

                var handleMethod = handler.GetType().GetMethod("Handle");

                tasks.Add((Task) handleMethod.Invoke(handler, new[] { message, this }));
            }

            return Task.WhenAll(tasks);
        }

        public Task Subscribe<TMessage>(IHandlerFactory<TMessage> factory) where TMessage : class
        {
            this.subscriptions.Add(new Subscription(typeof(TMessage), factory));
            
            return Task.CompletedTask;
        }

        public async Task WaitForCompletion()
        {
            while (this.messages.Count > 0)
            {
                var message = this.messages.Dequeue();
                await this.HandleMessage(message);
            }
        }
    }

    public interface IMessagingEndpoint : IMessageSession
    {
        Task Subscribe<TMessage>(IHandlerFactory<TMessage> factory) where TMessage : class;
    }

    public interface IHandlerFactory<in TMessage>
    {
        IHandleMessages<TMessage> Create();
    }

    public interface IHandleMessages<in TMessage>
    {
        Task Handle(TMessage message, IMessageHandlerContext context);
    }

    public interface IMessageHandlerContext
    {
        Task Send(object message);
        Task Publish(object message);
    }
    
    public interface IMessageSession
    {
        Task Send(object message);
        Task Publish(object message);
    }
}