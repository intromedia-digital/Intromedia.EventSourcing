using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<PackageContext>(op =>
{
    op.UseSqlServer(builder.Configuration["Sql"]!);
});

builder.Services.AddEventSourcing()
    .AddHandlersFromAssemblyContaining<Program>()
    //.UseDbContext<PackageContext>()
    .UseCosmos(
        builder.Configuration["Cosmos"]!,
        "event-sourcing"
    );


builder.Services.AddScoped<PackageRepository>();

builder.Services.AddHostedService<Initializer>();

builder.Services.AddHostedService<PackageProjection>();


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
    PackageReadModel state = await container.ReadItemAsync<PackageReadModel>(packageId.ToString(), new PartitionKey(packageId.ToString()));
    return state is not null ? Results.Ok(state) : Results.NotFound();
});


#endregion

app.Run();


internal sealed class PackageContext : DbContext
{
    public PackageContext(DbContextOptions<PackageContext> options) : base(options)
    {
    }
    public DbSet<PackageReadModel> Packages { get; set; }
    public DbSet<Projection> Projections => Set<Projection>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Projection>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Name).IsRequired();
            b.Property(p => p.StreamType).IsRequired();
            b.Property(p => p.Offset).IsRequired().HasDefaultValue(0);

            b.HasIndex(p => p.Name);
            b.HasIndex(p => p.StreamType);
        });

        modelBuilder.Entity<PackageReadModel>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.TrackingNumber).IsRequired();
            b.Property(p => p.CartId);
            b.Property(p => p.OutForDelivery);
            b.Property(p => p.Version).IsRequired();
        });

    }
}

internal sealed class Projection
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StreamType { get; set; } = string.Empty;
    public int Offset { get; set; } = 0;
    public Projection(string name, string streamType)
    {
        Name = name;
        StreamType = streamType;
    }
    private Projection()
    {
    }
}

