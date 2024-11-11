using Microsoft.Extensions.DependencyInjection;

public interface IEventSourcingBuilder
{
    IServiceCollection Services { get; }
}

