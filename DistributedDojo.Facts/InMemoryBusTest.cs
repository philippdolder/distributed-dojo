using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DistributedDojo.Facts
{
    public class InMemoryBusTest
    {
        private readonly InMemoryBus testee;

        public InMemoryBusTest()
        {
            this.testee = new InMemoryBus();
        }

        [Fact]
        public async Task SendsCommandToSubscriber()
        {
            var command = new Command();

            var messageStore = new MessageStore<Command>();
            await this.testee.Subscribe(new CommandHandlerFactory(messageStore));
            await this.testee.Send(command);
            
            await this.testee.WaitForCompletion();

            messageStore.Messages.Should().Contain(command);
        }

        [Fact]
        public async Task PublishesEventToSubscriber()
        {
            var @event = new Event();
            
            var messageStore = new MessageStore<Event>();
            await this.testee.Subscribe(new EventHandlerFactory(messageStore));
            await this.testee.Publish(@event);
            
            await this.testee.WaitForCompletion();

            messageStore.Messages.Should().Contain(@event);
        }

        [Fact]
        public async Task DeliversSubsequentMessages()
        {
            var command = new CommandPublishingEvent();
            
            var commandStore = new MessageStore<CommandPublishingEvent>();
            var eventStore = new MessageStore<Event>();
            await this.testee.Subscribe(new CommandPublishingEventFactory(commandStore));
            await this.testee.Subscribe(new EventHandlerFactory(eventStore));
            await this.testee.Publish(command);
            await this.testee.WaitForCompletion();

            eventStore.Messages.Should().HaveCount(1);
        }

        private class CommandPublishingEventFactory : IHandlerFactory<CommandPublishingEvent>
        {
            private readonly MessageStore<CommandPublishingEvent> store;

            public CommandPublishingEventFactory(MessageStore<CommandPublishingEvent> store)
            {
                this.store = store;
            }

            public IHandleMessages<CommandPublishingEvent> Create()
            {
                return new CommandPublishingEventHandler(this.store);
            }
        }

        private class CommandPublishingEventHandler : IHandleMessages<CommandPublishingEvent>
        {
            private readonly MessageStore<CommandPublishingEvent> store;

            public CommandPublishingEventHandler(MessageStore<CommandPublishingEvent> store)
            {
                this.store = store;
            }

            public Task Handle(CommandPublishingEvent message, IMessageHandlerContext context)
            {
                this.store.Add(message);

                return context.Publish(new Event());
            }
        }

        private class CommandPublishingEvent
        {
        }

        private class EventHandlerFactory : IHandlerFactory<Event>
        {
            private readonly MessageStore<Event> store;

            public EventHandlerFactory(MessageStore<Event> store)
            {
                this.store = store;
            }

            public IHandleMessages<Event> Create()
            {
                return new EventHandler(this.store);
            }
        }

        private class EventHandler : IHandleMessages<Event>
        {
            private readonly MessageStore<Event> store;

            public EventHandler(MessageStore<Event> store)
            {
                this.store = store;
            }

            public Task Handle(Event message, IMessageHandlerContext context)
            {
                this.store.Add(message);
                
                return Task.CompletedTask;
            }
        }
        
        private class Command
        {
        }

        private class CommandHandler : IHandleMessages<Command>
        {
            private readonly MessageStore<Command> store;

            public CommandHandler(MessageStore<Command> store)
            {
                this.store = store;
            }

            public Task Handle(Command message, IMessageHandlerContext context)
            {
                this.store.Add(message);
                
                return Task.CompletedTask;
            }
        }
        
        private class CommandHandlerFactory : IHandlerFactory<Command>
        {
            private readonly MessageStore<Command> store;

            public CommandHandlerFactory(MessageStore<Command> store)
            {
                this.store = store;
            }

            public IHandleMessages<Command> Create()
            {
                return new CommandHandler(this.store);
            }
        }

        private class MessageStore<TMessage> where TMessage : class
        {
            private readonly List<TMessage> messages = new List<TMessage>();
            
            public IReadOnlyList<TMessage> Messages => this.messages.AsReadOnly();
            
            public void Add(TMessage message) => this.messages.Add(message);
        }

        private class Event
        {
        }
    }
}