using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class DependencyInjection
{
    public static IEventSourcingBuilder UseDbContext<TContext>(this IEventSourcingBuilder builder)
        where TContext : DbContext
    {


        builder.Services.AddHostedService<Initializer<TContext>>();
        builder.Services.AddHostedService<Processor<TContext>>();

        builder.Services.RemoveAll<IEventStreams>();
        builder.Services.AddSingleton<IEventStreams, EventStreams<TContext>>();

        builder.Services.AddMediatR(c => {
            c.RegisterServicesFromAssembly(assembly: typeof(DependencyInjection).Assembly);
        });

        return builder;
    }

    public static ModelBuilder ConfigureEventSourcing(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>((Action<Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Event>>)(e =>
        {
            e.ToTable("Event");

            e.HasKey(p => p.Id)
                .HasName("PK_Event");

            e.Property(p => p.Id)
                .UseIdentityColumn()
                .HasColumnName("Id");

            e.Property(p => p.Timestamp)
                .IsRequired()
                .HasColumnName("Timestamp");

            e.Property(p => p.Payload)
                .IsRequired()
                .HasColumnName("Payload");

            e.Property(p => p.Published)
                .HasColumnName("Published");

            e.Property(p =>  p.StreamId)
                .IsRequired()
                .HasColumnName("StreamId");

            e.Property(p => p.Version)
                .IsRequired()
                .HasDefaultValue(0)
                .HasColumnName("Version");

            e.HasIndex(p => p.StreamId)
                .HasDatabaseName("IX_Event_StreamId");

            e.HasIndex(p => p.Published)
                .HasDatabaseName("IX_Event_Published");

            e.HasIndex(p => new { p.StreamId, p.Version })
                .HasDatabaseName("IX_Event_StreamId_Version");

            e.HasIndex(p => new { p.StreamId, p.Timestamp })
                .HasDatabaseName("IX_Event_StreamId_Timestamp");

        }));

        return modelBuilder;
    }
}

