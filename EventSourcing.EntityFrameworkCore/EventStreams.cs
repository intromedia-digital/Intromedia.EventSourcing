using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal sealed class EventStreams<TContext>(IServiceScopeFactory serviceScopeFactory) : IEventStreams
    where TContext : DbContext
{
    public async Task Append(Guid streamId, params IEvent[] events)
    {
        using var scope = serviceScopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<TContext>();
        using var transaction = context.Database.BeginTransaction();

        foreach (var @event in events)
        {
            await context.Set<Event>().AddAsync(new Event(streamId, @event));
        }

        await transaction.CommitAsync();
    }

    public async Task<TState> BuildState<TState>(Guid streamId) where TState : IState, new()
    {
        using var scope = serviceScopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<TContext>();

        var events = await context.Set<Event>()
            .AsNoTracking()
            .Where(e => e.StreamId == streamId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        var state = new TState()
        {
            Id = streamId
        };

        foreach (var @event in events)
        {
            var data = JsonConvert.DeserializeObject(@event.Payload, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
            if (data is IEvent evnt)
            {
                state.Apply(evnt);
            }
        }

        return state;
    }
}

