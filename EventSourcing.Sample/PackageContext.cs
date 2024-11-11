using Microsoft.EntityFrameworkCore;

internal sealed class PackageContext : DbContext
{
    public PackageContext(DbContextOptions<PackageContext> options) : base(options)
    {
    }
    public DbSet<PackageReadModel> Packages { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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

