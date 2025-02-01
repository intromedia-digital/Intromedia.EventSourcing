
using MediatR;
using System.Reflection;

namespace EventSourcing;
public interface IEvent 
{
    Guid Id { get; }
    int Version { get; }
}
