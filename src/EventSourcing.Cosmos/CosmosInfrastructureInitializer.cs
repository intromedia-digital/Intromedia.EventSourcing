using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;
using Microsoft.Extensions.Hosting;

namespace EventSourcing.Cosmos;
internal sealed class CosmosInfrastructureInitializer : IHostedService
{
    private readonly CosmosClient _cosmosClient;
    private readonly string _databaseId;
    private readonly List<string> _containerIds;

    public CosmosInfrastructureInitializer(CosmosClient cosmosClient, string databaseId, List<string> containerIds)
    {
        _cosmosClient = cosmosClient;
        _databaseId = databaseId;
        _containerIds = containerIds;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        DatabaseResponse databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseId, cancellationToken: cancellationToken);
        Database database = databaseResponse.Database;
        await database.CreateContainerIfNotExistsAsync(new ContainerProperties
        {
            Id = CosmosEventSourcingBuilder.LeaseContainerId,
            PartitionKeyPath = "/id"
        }, cancellationToken: cancellationToken);
        foreach (string containerId in _containerIds)
        {
            var uniqueKeyPolicy = new UniqueKeyPolicy();
            var uniqueKey = new UniqueKey();
            uniqueKey.Paths.Add("/streamId");
            uniqueKey.Paths.Add("/version");
            uniqueKeyPolicy.UniqueKeys.Add(uniqueKey);
            await database.CreateContainerIfNotExistsAsync(new ContainerProperties
            {
                Id = containerId,
                PartitionKeyPath = "/streamId",
                UniqueKeyPolicy = uniqueKeyPolicy

            }, cancellationToken: cancellationToken);

            var container = database.GetContainer(containerId);
            try
            {
                var sp = await container.Scripts.ReadStoredProcedureAsync("appendEvent", cancellationToken: cancellationToken);
            }
            catch (CosmosException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                await container.Scripts.CreateStoredProcedureAsync(new StoredProcedureProperties
                {
                    Id = "appendEvent",
                    Body = @"function InsertEventsWithVersioning(events){var context=getContext();var collection=context.getCollection();var response=context.getResponse();if(!Array.isArray(events)||events.length===0){throw new Error(""Input must be a non-empty array of events."")}
var streamId=events[0].streamId;for(var i=1;i<events.length;i++){if(events[i].streamId!==streamId){throw new Error(""All events must belong to the same stream."")}}
events.sort((a,b)=>a.version-b.version);var minVersion=events[0].version;var maxVersion=events[events.length-1].version;var query=`SELECT VALUE MAX(c.version) FROM c WHERE c.streamId = @streamId`;var parameters=[{name:""@streamId"",value:streamId}];var isAccepted=collection.queryDocuments(collection.getSelfLink(),{query:query,parameters:parameters},function(err,results){if(err){throw new Error(""Error querying existing events:""+err.message)}
var latestVersion=results.length>0?results[0]:null;if(latestVersion===null&&minVersion!==1){throw new Error(""The first event in a stream must have version 1."")}
if(latestVersion!==null&&minVersion!==latestVersion+1){throw new Error(`Invalid version sequence. Expected version ${latestVersion + 1}, but got ${minVersion}.`)}
for(var i=1;i<events.length;i++){if(events[i].version!==events[i-1].version+1){throw new Error(`Version gap detected. Expected ${events[i - 1].version + 1}, but got ${events[i].version}.`)}}
insertNext(0)});function insertNext(index){if(index>=events.length){response.setBody({message:""All events inserted successfully."",insertedCount:events.length});return}
var isInserted=collection.createDocument(collection.getSelfLink(),events[index],function(err,doc){if(err){throw new Error(""Error inserting event:""+err.message)}
insertNext(index+1)});if(!isInserted){throw new Error(""Insert operation not accepted by the server."")}}
if(!isAccepted){throw new Error(""Query operation not accepted by the server."")}}"
                }, cancellationToken: cancellationToken);
            }
        }
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}