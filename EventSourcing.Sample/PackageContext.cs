using Microsoft.EntityFrameworkCore;

internal sealed class PackageContext : DbContext
{
    public PackageContext(DbContextOptions<PackageContext> options) : base(options)
    {
    }
    public DbSet<PackageReadModel> Packages => Set<PackageReadModel>();
    public DbSet<ReceivedEvents> ReceivedEvents => Set<ReceivedEvents>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<ReceivedEvents>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Id).UseIdentityColumn();
            b.Property(p => p.EventId).IsRequired();
            b.HasIndex(p => p.EventId).IsUnique();
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

