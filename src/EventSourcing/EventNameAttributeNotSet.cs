namespace EventSourcing
{


public sealed class EventNameAttributeNotSet : Exception {
    public Type EventType { get; set; }
    public EventNameAttributeNotSet(Type eventType) {
        EventType = eventType;
    }
}

}
