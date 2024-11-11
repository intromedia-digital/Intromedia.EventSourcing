internal sealed class Projection
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StreamType { get; set; } = string.Empty;
    public int Offset { get; set; } = 0;
    public Projection(string name, string streamType)
    {
        Name = name;
        StreamType = streamType;
    }
    private Projection()
    {
    }
}

