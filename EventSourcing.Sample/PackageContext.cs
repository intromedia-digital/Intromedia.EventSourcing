using Microsoft.EntityFrameworkCore;

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

