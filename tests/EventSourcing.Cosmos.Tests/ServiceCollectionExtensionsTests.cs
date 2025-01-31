using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EventSourcing.Cosmos.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddsCorrectServicesToContainer()
    {
        IConfiguration cfg = new ConfigurationBuilder()
            .AddUserSecrets<ServiceCollectionExtensionsTests>()
            .Build();

        IServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(x =>
        {
            x.RegisterServicesFromAssemblyContaining<ServiceCollectionExtensionsTests>();
        });
           
        services.AddEventSourcing(e =>
        {
            e.UseCosmos(cosmos =>
            {
                cosmos.UseConnectionString(cfg.GetConnectionString("cosmos")!);
                cosmos.UseDatabase("test");
                cosmos.RegisterPolymorphicTypesFromAssemblyContaining<SampleEvent>();
                cosmos.AddStream("packages");
                cosmos.AddInMemoryPublisher();
                cosmos.ConfigureInfrastructure();
            });
        });

        IServiceProvider provider = services.BuildServiceProvider();

        var hostedServices = provider.GetServices<IHostedService>();

        foreach (var hostedService in hostedServices)
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        var eventStore = provider.GetRequiredService<IEventStore>();

        Assert.NotNull(eventStore);

        var id = Guid.NewGuid();

        var sampleEvent = new SampleEvent
        {
            Name = "test"
        };

        await eventStore.AppendToStreamAsync("packages", id, [new SampleEvent {
            StreamId = id,
            Id = Guid.NewGuid(),
            Version = 1,
            Name = "test"
        }]);

        await eventStore.AppendToStreamAsync("packages", id, [new SampleEvent {
            StreamId = id,
            Id = Guid.NewGuid(),
            Version = 2,
            Name = "test 2"
        }]);

        await Task.Delay(30000);

        var events = await eventStore.ReadStreamAsync("packages", id);

        Assert.Equal(2, events.Count());

        var firstEvent = events.First();

        Assert.Equal(1, firstEvent.Version);

        Assert.IsType<SampleEvent>(firstEvent);

        if (firstEvent is SampleEvent sample)
        {
            Assert.Equal("test", sample.Name);
        }

    }
}

[EventName("sample")]
public class SampleEvent : IEvent
{
    public string? Name { get; set; }
    public Guid StreamId { get; init; }
    public Guid Id { get; init; }
    public int Version { get; init; }
}

public class SampleEventHandler : INotificationHandler<SampleEvent>
{
    public Task Handle(SampleEvent notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class SampleProjection : Projection
{
    public SampleProjection(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
    public override string Name => "SampleProjection";
    public override Task ApplyAsync(IEvent @event, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}