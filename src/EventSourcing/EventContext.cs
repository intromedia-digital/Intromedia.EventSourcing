
using MediatR;
using System.Reflection;

namespace EventSourcing
{

    public sealed class EventContext<TEvent> : INotification
        where TEvent : IEvent
    {
        // Factory method to create EventContext with IEvent
        public EventContext<TEvent> Create(TEvent @event, Guid streamId)
        {
            return new EventContext<TEvent>(@event, streamId);
        }
        public EventContext(TEvent @event, Guid streamId)
        {
            Event = @event;
            StreamId = streamId;
        }

        public string Type => Event.GetType().GetCustomAttribute<EventNameAttribute>()?.EventName ?? string.Empty;
        public Guid StreamId { get; }
        public TEvent Event { get; }
    }
}
