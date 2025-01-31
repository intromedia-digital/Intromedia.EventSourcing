using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;
using System.Text.Json;

namespace EventSourcing.SqlServer;

internal sealed class InMemoryEventPublisherPollingProcessor<TProjection>(
    ILogger<InMemoryEventPublisherPollingProcessor<TProjection>> logger,
    DbConnectionFactory dbConnectionFactory,
    TProjection projection,
    JsonSerializerOptions jsonSerializerOptions
)
: BackgroundService
where TProjection : Projection
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var startFrom = new DateTime(1753, 1, 1);
     
        logger.LogInformation("Starting projection polling processor for {ProjectionName}", projection.Name);

        using var dbConnection = dbConnectionFactory.CreateConnection();

        // Check if subscription exists for the projection
        var subscriptionExists = await dbConnection.ExecuteScalarAsync<bool>(
            "SELECT COUNT(1) FROM Subscriptions WHERE StreamType = @StreamType AND Name = @Name",
            new { StreamType = "ALL", Name = projection.Name }
        );

        if (!subscriptionExists)
        {
            logger.LogInformation("Creating subscription for {ProjectionName}", projection.Name);
            // Seek first event in the stream type
            long? firstClusterKey = (await dbConnection.QueryFirstOrDefaultAsync<long?>(
                "SELECT TOP 1 ClusterKey FROM Streams ORDER BY ClusterKey"
            ) - 1) ?? -1;

            await dbConnection.ExecuteAsync(
                "INSERT INTO Subscriptions (StreamType, Name, Continuation) VALUES (@StreamType, @Name, @Continuation)",
                new { StreamType = "ALL", Name = projection.Name, Continuation = firstClusterKey }
            );
            logger.LogInformation("Created subscription for {ProjectionName}", projection.Name);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Processing events for {ProjectionName}", projection.Name);
            // Check subscription table to check what the last event was processed, ClusterKey (long)
            // Also check that the Name of the projection is the same as the SubscriptionName
            var lastEvent = await dbConnection.QueryFirstOrDefaultAsync<long>(
                "SELECT Continuation FROM Subscriptions WHERE StreamType = @StreamType AND Name = @Name",
                new { StreamType = "ALL", Name = projection.Name }
            );

            // If no events have been processed, check startFrom date
            if (lastEvent == -1)
            {
                logger.LogInformation("No events processed for {ProjectionName}, checking startFrom date", projection.Name);
                long? lastClusterKey = await dbConnection.QueryFirstOrDefaultAsync<long?>(
                    "SELECT TOP 1 ClusterKey FROM Streams WHERE StreamType = @StreamType AND Created >= @Created ORDER BY ClusterKey",
                    new { StreamType = "ALL", Created = startFrom }
                );

                lastEvent = (lastClusterKey - 1) ?? -1;

                if (lastEvent == -1)
                {
                    logger.LogInformation("No events to process for {ProjectionName}", projection.Name);
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }
            }

            lastEvent++;

            // Read next event (1) from the last processed event
            var events = await dbConnection.QueryAsync<SqlEventData>(
                "SELECT TOP 1 * FROM Streams WHERE ClusterKey = @ClusterKey ORDER BY ClusterKey",
                new { ClusterKey = lastEvent }
            );

            if (!events.Any())
            {
                logger.LogInformation("No events to process for {ProjectionName}", projection.Name);
                await Task.Delay(1000, stoppingToken);
                continue;
            }

            logger.LogInformation("Processing {EventCount} events for {ProjectionName}", events.Count(), projection.Name);

            foreach (var @event in events)
            {
                await projection.ApplyAsync(@event.ToEvent(jsonSerializerOptions));
            }


            logger.LogInformation("Processed {EventCount} events for {ProjectionName}", events.Count(), projection.Name);
            // Update subscription table with the last processed event
            await dbConnection.ExecuteAsync(
                "UPDATE Subscriptions SET Continuation = @Continuation WHERE StreamType = @StreamType AND Name = @Name",
                new { Continuation = lastEvent, StreamType = "ALL", Name = projection.Name }
            );
            logger.LogInformation("Updated subscription for {ProjectionName} to {LastEvent}", projection.Name, lastEvent);

            await Task.Delay(1000, stoppingToken);
        }

        logger.LogInformation("Stopping projection polling processor for {ProjectionName}", projection.Name);
    }
}
