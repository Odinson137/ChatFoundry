using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WorkflowService.Models.Workflow;

namespace WorkflowService.Utils;

public sealed class WorkflowConditionJsonConverter 
    : JsonConverter<WorkflowCondition>
{
    public override WorkflowCondition ReadJson(
        JsonReader reader,
        Type objectType,
        WorkflowCondition? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        var jo = JObject.Load(reader);

        if (jo.Property("equals") != null)
            return jo["equals"]!.ToObject<EqualsCondition>(serializer)!;

        if (jo.Property("and") != null)
            return new AndCondition
            {
                Conditions = jo["and"]!
                    .ToObject<List<WorkflowCondition>>(serializer)!
            };

        if (jo.Property("or") != null)
            return new OrCondition
            {
                Conditions = jo["or"]!
                    .ToObject<List<WorkflowCondition>>(serializer)!
            };

        throw new JsonSerializationException(
            $"Unknown condition type: {jo}");
    }

    public override void WriteJson(
        JsonWriter writer,
        WorkflowCondition? value,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
