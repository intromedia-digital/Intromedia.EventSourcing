using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        services.AddEventSourcing(e =>
        {
            //e.UseKeyedServices("serviceKey");

            e.UseCosmos(cosmos =>
            {
                cosmos.UseConnectionString(cfg.GetConnectionString("cosmos")!);
                cosmos.UseDatabase("test");
                cosmos.RegisterPolymorphicTypesFromAssemblyContaining<SampleEvent>();

                cosmos.AddStream("packages");

                cosmos.ConfigureInfrastructure();
            });


            //e.AddEventualProjection<IProjection>();
            //e.AddLiveProjection<IProjection>();

            //e.AddInMemoryPublisher();
        });

        IServiceProvider provider = services.BuildServiceProvider();

        var initializer = provider.GetRequiredService<CosmosInfrastructureInitializer>();

        await initializer.StartAsync(default);

        var eventStore = provider.GetRequiredService<IEventStore>();

        Assert.NotNull(eventStore);

        var id = Guid.NewGuid();
        var packageStream = await eventStore.OpenStream("packages", id);

        Assert.NotNull(packageStream);


        var sampleEvent = new SampleEvent
        {
            Name = "test"
        };

        await packageStream.AppendToStreamAsync([new Event(Guid.NewGuid(), 1, sampleEvent)]);
        await packageStream.AppendToStreamAsync([new Event(Guid.NewGuid(), 2, sampleEvent)]);


        var events = await packageStream.ReadStreamAsync();

        Assert.Equal(2, events.Count());

        var firstEvent = events.First();

        Assert.Equal(1, firstEvent.Version);

        Assert.IsType<SampleEvent>(firstEvent.Data);

        if(firstEvent.Data is SampleEvent sample)
        {
            Assert.Equal("test", sample.Name);
        }






    }
}

[EventName("sample")]
public class SampleEvent : IEventData
{
    public string? Name { get; set; }
}