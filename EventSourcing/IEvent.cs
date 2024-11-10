using MediatR;

public interface IEvent : INotification
{
    int Version { get; }
}


