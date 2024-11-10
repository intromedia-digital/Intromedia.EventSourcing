using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<PackageContext>(op =>
{
    op.UseSqlServer(builder.Configuration["Sql"]!);
});

builder.Services.AddEventSourcing()
    .AddHandlersFromAssemblyContaining<Program>()
    .UseDbContext<PackageContext>()
    .UseCosmos(
        builder.Configuration["Cosmos"]!,
        "event-sourcing"
    );


builder.Services.AddScoped<PackageRepository>();

builder.Services.AddHostedService<Initializer>();


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

app.MapGet("packages/{packageId:guid}", async (Guid packageId, CosmosClient cosmos) =>
{
    var container = cosmos.GetContainer("event-sourcing", "packages");
    PackageState state = await container.ReadItemAsync<PackageState>(packageId.ToString(), new PartitionKey(packageId.ToString()));
    return state is not null ? Results.Ok(state) : Results.NotFound();
});

app.MapGet("carts/{cartId:guid}", async (Guid cartId, CosmosClient cosmos) =>
{
    var container = cosmos.GetContainer("event-sourcing", "carts");
    Cart cart = await container.ReadItemAsync<Cart>(cartId.ToString(), new PartitionKey(cartId.ToString()));
    return cart is not null ? Results.Ok(cart) : Results.NotFound();
});

#endregion

app.Run();
