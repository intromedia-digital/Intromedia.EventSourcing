using EntityFramework.Exceptions.SqlServer;
using EventSourcing;
using EventSourcing.Cosmos;
using EventSourcing.Cosmos.Tests;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using static Azure.Core.HttpHeader;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(x =>
{
    x.RegisterServicesFromAssemblyContaining<SampleEventHandler>();
});

builder.Services.AddEventSourcing(e =>
{
    e.UseCosmos(c =>
    {
        c.UseConnectionString(builder.Configuration.GetConnectionString("cosmos")!);
        c.UseDatabase("test");
        c.RegisterPolymorphicTypesFromAssemblyContaining<SampleEvent>();
        c.AddStream(Streams.Packages);
        c.AddInMemoryPublisher();
        c.ConfigureInfrastructure();
        c.AddProjection<SampleProjection>(Streams.Packages, DateTime.MinValue);
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

    var sampleEvent = new SampleEvent
    {
        Name = "test"
    };

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
}


