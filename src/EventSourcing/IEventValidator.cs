

using System.ComponentModel.DataAnnotations;

namespace EventSourcing;

public static class IEventValidator
{
    /// <summary>
    /// Validate the event
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="event"></param>
    /// <exception cref="ValidationException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static void Validate<TEvent>(TEvent @event) where TEvent : IEvent
    {
        if (@event is null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        if (@event.Id.Equals(Guid.Empty))
        {
            throw new ValidationException("Id is required");
        }

        if (@event.Version < 1)
        {
            throw new ValidationException("Version is required");
        }
    }
}

