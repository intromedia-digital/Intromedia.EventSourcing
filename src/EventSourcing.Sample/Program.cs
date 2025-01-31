using EventSourcing;
using EventSourcing.Cosmos;
using EventSourcing.Cosmos.Tests;
using EventSourcing.SqlServer;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(x =>
{
    x.RegisterServicesFromAssemblyContaining<SampleEventHandler>();
});

builder.Services.AddEventSourcing(e =>
{
    e.RegisterPolymorphicTypesFromAssemblyContaining<SampleEvent>();

    //e.UseCosmos(c =>
    //{
    //    c.UseConnectionString(builder.Configuration.GetConnectionString("cosmos")!);
    //    c.UseDatabase("test");

    //    c.AddStream(Streams.Packages);
    //    c.AddStream(Streams.Orders);

    //    c.ConfigureInfrastructure();

    //    c.AddInMemoryPublisher();
    //    c.AddProjection<SampleProjection>(Streams.Packages, startFrom: DateTime.MinValue);

    //});

    e.UseSqlServer(s =>
    {
        s.UseConnectionString(builder.Configuration.GetConnectionString("sql")!);

        s.ConfigureInfrastructure();

        s.AddInMemoryPublisher();

        s.AddProjection<SampleProjection>(Streams.Packages, startFrom: DateTime.MinValue);
    });


});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

#region Endpoints


app.MapGet("packages", async (IEventStore eventStore) =>
{
    var id = Guid.NewGuid();

    try
    {
        IEventValidator.Validate(new SampleEvent
            {
                Name = "test"
            }
        );
    }
    catch (ValidationException)
    {
    }

    await eventStore.AppendToStreamAsync(Streams.Packages, id, [new SampleEvent {
            StreamId = id,
            Id = Guid.NewGuid(),
            Version = 1,
            Name = "test"
        }]);

    return Results.NoContent();
});


#endregion

app.Run();


internal static class Streams
{
    public const string Packages = "packages";
    public const string Orders = "orders";

}


