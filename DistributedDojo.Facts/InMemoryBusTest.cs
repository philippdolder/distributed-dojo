using System;
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
        public async Task SendsCommandToAllSubscribers()
        {
            var command = new Command();

            var firstMessageStore = new MessageStore<Command>();
            var secondMessageStore = new MessageStore<Command>();
            await this.testee.Subscribe(new CommandHandlerFactory(firstMessageStore));
            await this.testee.Subscribe(new SecondCommandHandlerFactory(secondMessageStore));
            await this.testee.Send(command);
            
            await this.testee.WaitForCompletion();

            firstMessageStore.Messages.Should().Contain(command);
            secondMessageStore.Messages.Should().Contain(command);
        }

        [Fact]
        public async Task PublishesEventToAllSubscribers()
        {
            var @event = new Event();
            
            var firstMessageStore = new MessageStore<Event>();
            var secondMessageStore = new MessageStore<Event>();
            await this.testee.Subscribe(new EventHandlerFactory(firstMessageStore));
            await this.testee.Subscribe(new SecondEventHandlerFactory(secondMessageStore));
            await this.testee.Publish(@event);
            
            await this.testee.WaitForCompletion();

            firstMessageStore.Messages.Should().Contain(@event);
            secondMessageStore.Messages.Should().Contain(@event);
        }

        [Fact]
        public async Task DeliversSubsequentMessage()
        {
            var command = new CommandPublishingEvent();
            
            var commandStore = new MessageStore<CommandPublishingEvent>();
            var eventStore = new MessageStore<Event>();
            await this.testee.Subscribe(new CommandPublishingEventHandlerFactory(commandStore));
            await this.testee.Subscribe(new EventHandlerFactory(eventStore));
            await this.testee.Publish(command);
            await this.testee.WaitForCompletion();

            eventStore.Messages.Should().HaveCount(1);
        }

        [Fact]
        public async Task ThrowsExceptionWhenSendingAnEvent()
        {
            var @event = new Event();

            Func<Task> act = () => this.testee.Send(@event);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task ThrowsExceptionWhenPublishingACommand()
        {
            var command = new Command();

            Func<Task> act = () => this.testee.Publish(command);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        private class Command
        {
        }

        private class Event
        {
        }

        private class CommandPublishingEvent
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

        private class SecondCommandHandler : IHandleMessages<Command>
        {
            private readonly MessageStore<Command> store;

            public SecondCommandHandler(MessageStore<Command> store)
            {
                this.store = store;
            }

            public Task Handle(Command message, IMessageHandlerContext context)
            {
                this.store.Add(message);

                return Task.CompletedTask;
            }
        }

        private class SecondCommandHandlerFactory : IHandlerFactory<Command>
        {
            private readonly MessageStore<Command> store;

            public SecondCommandHandlerFactory(MessageStore<Command> store)
            {
                this.store = store;
            }

            public IHandleMessages<Command> Create()
            {
                return new SecondCommandHandler(this.store);
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

        private class SecondEventHandler : IHandleMessages<Event>
        {
            private readonly MessageStore<Event> store;

            public SecondEventHandler(MessageStore<Event> store)
            {
                this.store = store;
            }

            public Task Handle(Event message, IMessageHandlerContext context)
            {
                this.store.Add(message);

                return Task.CompletedTask;
            }
        }

        private class SecondEventHandlerFactory : IHandlerFactory<Event>
        {
            private readonly MessageStore<Event> store;

            public SecondEventHandlerFactory(MessageStore<Event> store)
            {
                this.store = store;
            }

            public IHandleMessages<Event> Create()
            {
                return new SecondEventHandler(this.store);
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

        private class CommandPublishingEventHandlerFactory : IHandlerFactory<CommandPublishingEvent>
        {
            private readonly MessageStore<CommandPublishingEvent> store;

            public CommandPublishingEventHandlerFactory(MessageStore<CommandPublishingEvent> store)
            {
                this.store = store;
            }

            public IHandleMessages<CommandPublishingEvent> Create()
            {
                return new CommandPublishingEventHandler(this.store);
            }
        }

        private class MessageStore<TMessage> where TMessage : class
        {
            private readonly List<TMessage> messages = new List<TMessage>();
            
            public IReadOnlyList<TMessage> Messages => this.messages.AsReadOnly();
            
            public void Add(TMessage message) => this.messages.Add(message);
        }
    }
}