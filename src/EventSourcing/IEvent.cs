
using MediatR;

namespace EventSourcing;

public interface IEvent : INotification {
    Guid StreamId { get; }
    Guid Id { get; }
    int Version { get; }
}

