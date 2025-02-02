namespace EventSourcing
{


    public sealed class EventNameAttribute : Attribute
    {
        public string EventName { get; set; }

        public EventNameAttribute(string eventName)
        { EventName = eventName; }

    }
}
