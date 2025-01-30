using Microsoft.Extensions.DependencyInjection;
using System;
public interface IEventSourcingBuilder
{
    void UseKeyedServices(object serviceKey);
    object ServiceKey { get; }
    bool ShouldUseKeyedServices { get; }
    IServiceCollection Services { get; }
}

