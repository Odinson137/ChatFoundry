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
        if (jo.Property("notEquals") != null)
            return jo["notEquals"]!.ToObject<NotEqualsCondition>(serializer)!;
        if (jo.Property("contains") != null)
            return jo["contains"]!.ToObject<ContainsCondition>(serializer)!;
        if (jo.Property("greaterThan") != null)
            return jo["greaterThan"]!.ToObject<GreaterThanCondition>(serializer)!;
        if (jo.Property("lessThan") != null)
            return jo["lessThan"]!.ToObject<LessThanCondition>(serializer)!;
        if (jo.Property("greaterOrEqual") != null)
            return jo["greaterOrEqual"]!.ToObject<GreaterOrEqualCondition>(serializer)!;
        if (jo.Property("lessOrEqual") != null)
            return jo["lessOrEqual"]!.ToObject<LessOrEqualCondition>(serializer)!;
        if (jo.Property("startsWith") != null)
            return jo["startsWith"]!.ToObject<StartsWithCondition>(serializer)!;
        if (jo.Property("endsWith") != null)
            return jo["endsWith"]!.ToObject<EndsWithCondition>(serializer)!;
        if (jo.Property("regex") != null)
            return jo["regex"]!.ToObject<RegexMatchCondition>(serializer)!;
        if (jo.Property("inList") != null)
            return jo["inList"]!.ToObject<InListCondition>(serializer)!;
        if (jo.Property("isEmpty") != null)
            return jo["isEmpty"]!.ToObject<IsEmptyCondition>(serializer)!;
        if (jo.Property("isNotEmpty") != null)
            return jo["isNotEmpty"]!.ToObject<IsNotEmptyCondition>(serializer)!;
        if (jo.Property("not") != null)
            return new NotCondition { Condition = jo["not"]!.ToObject<WorkflowCondition>(serializer)! };

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
        if (value == null)
        {
            writer.WriteNull();
            return;
        }
        static object BinObj(string left, string right, bool? ignoreCase) =>
            ignoreCase.HasValue ? new { left, right, ignoreCase = ignoreCase.Value } : new { left, right };

        var jo = value switch
        {
            EqualsCondition eq => JObject.FromObject(new { equals = BinObj(eq.Left, eq.Right, eq.IgnoreCase) }, serializer),
            NotEqualsCondition ne => JObject.FromObject(new { notEquals = BinObj(ne.Left, ne.Right, ne.IgnoreCase) }, serializer),
            ContainsCondition c => JObject.FromObject(new { contains = BinObj(c.Left, c.Right, c.IgnoreCase) }, serializer),
            GreaterThanCondition gt => JObject.FromObject(new { greaterThan = BinObj(gt.Left, gt.Right, gt.IgnoreCase) }, serializer),
            LessThanCondition lt => JObject.FromObject(new { lessThan = BinObj(lt.Left, lt.Right, lt.IgnoreCase) }, serializer),
            GreaterOrEqualCondition ge => JObject.FromObject(new { greaterOrEqual = BinObj(ge.Left, ge.Right, ge.IgnoreCase) }, serializer),
            LessOrEqualCondition le => JObject.FromObject(new { lessOrEqual = BinObj(le.Left, le.Right, le.IgnoreCase) }, serializer),
            StartsWithCondition sw => JObject.FromObject(new { startsWith = BinObj(sw.Left, sw.Right, sw.IgnoreCase) }, serializer),
            EndsWithCondition ew => JObject.FromObject(new { endsWith = BinObj(ew.Left, ew.Right, ew.IgnoreCase) }, serializer),
            RegexMatchCondition rx => JObject.FromObject(new { regex = BinObj(rx.Left, rx.Right, rx.IgnoreCase) }, serializer),
            InListCondition il => JObject.FromObject(new { inList = BinObj(il.Left, il.Right, il.IgnoreCase) }, serializer),
            IsEmptyCondition ie => JObject.FromObject(new { isEmpty = new { ie.Left } }, serializer),
            IsNotEmptyCondition ine => JObject.FromObject(new { isNotEmpty = new { ine.Left } }, serializer),
            NotCondition n => new JObject { ["not"] = JToken.FromObject(n.Condition, serializer) },
            AndCondition and => new JObject { ["and"] = JArray.FromObject(and.Conditions, serializer) },
            OrCondition or => new JObject { ["or"] = JArray.FromObject(or.Conditions, serializer) },
            _ => throw new JsonSerializationException($"Condition {value.GetType().Name} is not supported for write")
        };
        jo.WriteTo(writer);
    }
}
