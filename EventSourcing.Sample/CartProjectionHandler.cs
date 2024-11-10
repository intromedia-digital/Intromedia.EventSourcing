using Microsoft.Azure.Cosmos;

internal sealed class CartProjectionHandler(CosmosClient cosmos) :
    IEventHandler<PackageLoadedOnCart>
{
    private readonly Container db = cosmos.GetContainer("event-sourcing", "carts");
    public async Task Handle(PackageLoadedOnCart notification, CancellationToken cancellationToken)
    {
        Cart cart;
        try
        {
            cart = await db.ReadItemAsync<Cart>(notification.CartId.ToString(), new PartitionKey(notification.CartId.ToString()));
        }
        catch (CosmosException e) when (e.StatusCode is System.Net.HttpStatusCode.NotFound)
        {
            cart = new Cart
            {
                Id = notification.CartId
            };
        }

        cart.Apply(notification);

        await db.UpsertItemAsync(cart, new PartitionKey(cart.Id.ToString()));
    }
}
