using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

public class PolymorphicTypeResolver(JsonDerivedType[] types) : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        JsonTypeInfo jsonTypeInfo = base.GetTypeInfo(type, options);


        if (jsonTypeInfo.Type != typeof(IEventData))
            return jsonTypeInfo;

        var polymorphismOptions = new JsonPolymorphismOptions();

        foreach (var eventType in types)
        {
            polymorphismOptions.DerivedTypes.Add(eventType);
        }

        jsonTypeInfo.PolymorphismOptions = polymorphismOptions;

        return jsonTypeInfo;
    }
}

