using EntityFramework.Exceptions.SqlServer;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<PackageContext>(op =>
{
    op.UseSqlServer(builder.Configuration["Sql"]!).UseExceptionProcessor();
});

builder.Services.AddEventSourcing()
    .UseCosmos(
        connectionString: builder.Configuration["Cosmos"]!,
        databaseId: "event-sourcing",
        serviceKey: ServiceKey.Key
    )
    .AddEventTypesFromAssemblies(typeof(Program).Assembly)
    .AddAppendStream<PackageStream>()
    .AddAppendStream<Packge2Stream>()
    .AddSubscription<PackageSubscription, PackageStream>();

builder.Services.AddEventSourcing()
    .UseCosmos(
    connectionString: builder.Configuration["Cosmos"]!,
    databaseId: "event-sourcing-db2",
    serviceKey: "OtherServiceKey"
    )
    .AddEventTypesFromAssemblies(typeof(Program).Assembly)
    .AddAppendStream<PackageStream>();

builder.Services.AddScoped<PackageRepository>();
builder.Services.AddSingleton<PackageProjection>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

#region Endpoints
app.MapPost("packages", async (PackageRepository packageRepository) =>
{
    var packageId = Guid.NewGuid();

    var package = new PackageAggregate(packageId, "ABC123");

    await packageRepository.Save(package);

    return Results.Ok(packageId);
});

app.MapPost("packages/{packageId:guid}/load", async (Guid packageId, Guid cartId, PackageRepository packageRepository) =>
{
    var package = await packageRepository.Get(packageId);

    package.LoadOnCart(cartId);

    await packageRepository.Save(package);

    return Results.NoContent();
});

app.MapPost("packages/{packageId:guid}/begindelivery", async (Guid packageId, PackageRepository packageRepository) =>
{
    var package = await packageRepository.Get(packageId);

    package.BeginDelivery();

    await packageRepository.Save(package);

    return Results.NoContent();
});

app.MapGet("packages/{packageId:guid}", async (Guid packageId, [FromKeyedServices(ServiceKey.Key)] CosmosClient cosmos) =>
{
    var container = cosmos.GetContainer("event-sourcing", "package");
});


#endregion

app.Run();




public static class ServiceKey
{
    public const string Key = "ServiceKey1";

}