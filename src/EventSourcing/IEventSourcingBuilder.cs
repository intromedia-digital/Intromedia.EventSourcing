using Microsoft.Extensions.DependencyInjection;
using System;
namespace EventSourcing;

public interface IEventSourcingBuilder
{
    void UseKeyedServices(object serviceKey);
    object ServiceKey { get; }
    bool ShouldUseKeyedServices { get; }
    IServiceCollection Services { get; }
}

