internal sealed class Cart
{
    public Guid Id { get; set; }
    public IList<string> Packages { get; set; } = new List<string>();
    public void Apply(PackageLoadedOnCart packageLoadedOnCart)
    {
        if (Packages.Contains(packageLoadedOnCart.PackageId.ToString()))
        {
            throw new InvalidOperationException("Package is already loaded on a cart");
        }
        Packages.Add(packageLoadedOnCart.PackageId.ToString());
    }
}
