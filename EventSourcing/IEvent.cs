using MediatR;

public interface IEvent : INotification
{
    Guid EventId { get; }
    int Version { get; }
}


