using Microsoft.EntityFrameworkCore;

internal class PackageContext : DbContext
{
    // dotnet ef migrations add Initial -p EventSourcing.Sample -c PackageContext
    public PackageContext(DbContextOptions<PackageContext> options) : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ConfigureEventSourcing();
    }

}
