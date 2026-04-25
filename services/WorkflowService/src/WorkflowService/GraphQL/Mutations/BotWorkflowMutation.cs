using System.Text.Json;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Scheduler.Grpc;
using Shared.Infrastructure.GraphQl;
using WorkflowService.Data;
using WorkflowService.Entities;
using WorkflowService.Enums;
using WorkflowService.Models.Node;

namespace WorkflowService.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class BotWorkflowMutation
{
    public async Task<AddBotWorkflowPayload> AddBotWorkflowAsync(
        AddBotWorkflowInput input,
        [Service] WorkflowDbContext context,
        [Service] SchedulerGrpcService.SchedulerGrpcServiceClient? schedulerClient = null)
    {
        var workflow = new BotWorkflow
        {
            BotId = input.BotId,
            NodesDefinition = input.NodesDefinition,
            EdgesDefinition = input.EdgesDefinition,
            LayoutDefinition = input.LayoutDefinition,
            Version = input.Version,
            IsActiveBotWorkflow = input.IsActiveBotWorkflow,
            InputParametersDefinition = input.InputParametersDefinition ?? "[]",
            OutputParametersDefinition = input.OutputParametersDefinition ?? "[]"
        };

        context.Workflows.Add(workflow);
        if (input.IsActiveBotWorkflow)
            await context.Workflows.Where(w => w.BotId == input.BotId).ExecuteUpdateAsync(s => s.SetProperty(w => w.IsActiveBotWorkflow, false));
        await context.SaveChangesAsync();

        if (workflow.IsActiveBotWorkflow)
            await SyncTimerStartScheduleAsync(workflow, context, schedulerClient);

        return new AddBotWorkflowPayload(workflow);
    }

    public async Task<UpdateBotWorkflowPayload> UpdateBotWorkflowAsync(
        UpdateBotWorkflowInput input,
        [Service] WorkflowDbContext context,
        [Service] SchedulerGrpcService.SchedulerGrpcServiceClient? schedulerClient = null)
    {
        var workflow = await context.Workflows
            .Include(w => w.Bot)
            .ThenInclude(b => b.BotChannels)
            .FirstOrDefaultAsync(w => w.Id == input.WorkflowId);

        if (workflow is null)
        {
            return new UpdateBotWorkflowPayload(null);
        }

        workflow.NodesDefinition = input.NodesDefinition ?? workflow.NodesDefinition;
        workflow.EdgesDefinition = input.EdgesDefinition ?? workflow.EdgesDefinition;
        workflow.LayoutDefinition = input.LayoutDefinition ?? workflow.LayoutDefinition;
        workflow.Version = input.Version ?? workflow.Version;
        workflow.IsActiveBotWorkflow = input.IsActiveBotWorkflow ?? workflow.IsActiveBotWorkflow;
        workflow.ModifiedAt = DateTime.UtcNow;

        if (input.InputParametersDefinition != null)
            workflow.InputParametersDefinition = input.InputParametersDefinition;
        if (input.OutputParametersDefinition != null)
            workflow.OutputParametersDefinition = input.OutputParametersDefinition;

        if (workflow.IsActiveBotWorkflow)
            await context.Workflows.Where(w => w.BotId == workflow.BotId && w.Id != workflow.Id).ExecuteUpdateAsync(s => s.SetProperty(w => w.IsActiveBotWorkflow, false));

        await context.SaveChangesAsync();

        await SyncTimerStartScheduleAsync(workflow, context, schedulerClient);

        return new UpdateBotWorkflowPayload(workflow);
    }

    public async Task<DeleteBotWorkflowPayload> DeleteBotWorkflowAsync(
        DeleteBotWorkflowInput input,
        [Service] WorkflowDbContext context,
        [Service] SchedulerGrpcService.SchedulerGrpcServiceClient? schedulerClient = null)
    {
        var workflow = await context.Workflows.FindAsync(input.WorkflowId);

        if (workflow is null)
        {
            return new DeleteBotWorkflowPayload(null);
        }

        await UnregisterTimerStartAsync(workflow.Id, schedulerClient);

        context.Workflows.Remove(workflow);
        await context.SaveChangesAsync();

        return new DeleteBotWorkflowPayload(workflow);
    }

    public async Task<CopyBotWorkflowPayload> CopyBotWorkflowAsync(
        CopyBotWorkflowInput input,
        [Service] WorkflowDbContext context)
    {
        var source = await context.Workflows
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == input.SourceWorkflowId);

        if (source is null)
            return new CopyBotWorkflowPayload(null);

        var maxVersion = await context.Workflows
            .Where(w => w.BotId == source.BotId)
            .MaxAsync(w => (int?)w.Version);
        var nextVersion = (maxVersion ?? 0) + 1;

        var copy = new BotWorkflow
        {
            BotId = source.BotId,
            NodesDefinition = source.NodesDefinition,
            EdgesDefinition = source.EdgesDefinition,
            LayoutDefinition = source.LayoutDefinition,
            Version = nextVersion,
            IsActiveBotWorkflow = false,
            InputParametersDefinition = source.InputParametersDefinition,
            OutputParametersDefinition = source.OutputParametersDefinition
        };

        context.Workflows.Add(copy);
        await context.SaveChangesAsync();

        return new CopyBotWorkflowPayload(copy);
    }

    private static async Task SyncTimerStartScheduleAsync(
        BotWorkflow workflow,
        WorkflowDbContext context,
        SchedulerGrpcService.SchedulerGrpcServiceClient? schedulerClient)
    {
        if (schedulerClient == null)
            return;

        var jobKey = $"timer:{workflow.Id}";
        var timerNode = FindTimerStartNode(workflow.NodesDefinition);

        // if (!workflow.IsActiveBotWorkflow || timerNode == null || !timerNode.HasValue)
        // {
           var test = UnregisterTimerStartAsync(workflow.Id, schedulerClient);
            // return;
        // }

        var timerElement = timerNode.Value;
        var data = timerElement.GetProperty("data");
        var scheduleType = data.TryGetProperty("scheduleType", out var st) ? st.GetString() ?? "OneTime" : "OneTime";
        var fireTime = data.TryGetProperty("fireTimeUtc", out var ft) ? ft.GetString() : null;
        var cronExpression = data.TryGetProperty("cronExpression", out var ce) ? ce.GetString() : null;
        var timezone = data.TryGetProperty("timezone", out var tz) ? tz.GetString() ?? "UTC" : "UTC";

        // Convert local fire time + timezone to UTC
        string? fireTimeUtc = null;
        if (!string.IsNullOrEmpty(fireTime) && scheduleType.Equals("OneTime", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                var localTime = DateTimeOffset.Parse(fireTime);
                fireTimeUtc = TimeZoneInfo.ConvertTime(localTime, tzInfo).UtcDateTime.ToString("O");
            }
            catch
            {
                fireTimeUtc = fireTime;
            }
        }

        // Process clientFilterJson: convert channel GUIDs to int channel types
        var clientFilterJson = data.TryGetProperty("clientFilter", out var cf)
            ? cf.ValueKind != JsonValueKind.Null ? cf.GetRawText() : null
            : null;

        if (!string.IsNullOrEmpty(clientFilterJson))
        {
            try
            {
                var filterDoc = JsonDocument.Parse(clientFilterJson);
                var filterObj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(clientFilterJson);
                if (filterObj != null && filterObj.TryGetValue("channels", out var channelsEl) && channelsEl.ValueKind == JsonValueKind.Array)
                {
                    var channelGuids = channelsEl.EnumerateArray()
                        .Select(e => Guid.TryParse(e.GetString(), out var g) ? g : (Guid?)null)
                        .Where(g => g.HasValue)
                        .Select(g => g!.Value)
                        .ToList();

                    if (channelGuids.Count > 0)
                    {
                        var channelTypeInts = await context.MessengerChannels
                            .Where(c => channelGuids.Contains(c.Id))
                            .Select(c => (int)c.ChannelType)
                            .Distinct()
                            .ToListAsync();

                        filterObj["channels"] = JsonSerializer.SerializeToElement(channelTypeInts);
                        clientFilterJson = JsonSerializer.Serialize(filterObj);
                    }
                }
            }
            catch
            {
                // Keep original JSON if processing fails
            }
        }

        // Get first bot channel for the scheduler to publish messages
        string? channelId = null;
        string? channel = null;
        if (workflow.Bot?.BotChannels?.Count > 0)
        {
            var botChannel = workflow.Bot.BotChannels.First();
            channelId = botChannel.ChannelId.ToString();
            //channel = botChannel.Channel.ToString();
        }

        if (channelId == null)
            return;

        try
        {
            await test;
            
            await schedulerClient.RegisterTimerStartAsync(new RegisterTimerStartRequest
            {
                JobKey = jobKey,
                ScheduleType = scheduleType,
                FireTimeUtc = fireTimeUtc ?? "",
                CronExpression = cronExpression ?? "",
                Timezone = timezone,
                ClientFilterJson = clientFilterJson ?? "",
                WorkflowId = workflow.Id.ToString(),
                BotId = workflow.BotId.ToString(),
                ChannelId = channelId,
                CompanyId = (workflow.Bot?.CompanyId ?? Guid.Empty).ToString(),
                Channel = channelId,
            });
        }
        catch
        {
            // Best-effort: scheduler might be unavailable
        }
    }

    private static async Task UnregisterTimerStartAsync(
        Guid workflowId,
        SchedulerGrpcService.SchedulerGrpcServiceClient? schedulerClient)
    {
        if (schedulerClient == null)
            return;

        try
        {
            await schedulerClient.UnregisterTimerStartAsync(new UnregisterTimerStartRequest
            {
                JobKey = $"timer:{workflowId}",
            });
        }
        catch
        {
            // Best-effort
        }
    }

    private static JsonElement? FindTimerStartNode(string nodesJson)
    {
        if (string.IsNullOrWhiteSpace(nodesJson))
            return null;

        var doc = JsonDocument.Parse(nodesJson);
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            if (el.TryGetProperty("type", out var t) &&
                t.ValueKind == JsonValueKind.String &&
                t.GetString() == "TimerStart")
                return el;
        }

        return null;
    }
}

#region Records for GraphQL

public record AddBotWorkflowInput(
    Guid BotId, 
    string NodesDefinition, 
    string EdgesDefinition, 
    string LayoutDefinition, 
    int Version = 1, 
    bool IsActiveBotWorkflow = false,
    string? InputParametersDefinition = null,
    string? OutputParametersDefinition = null);

public record AddBotWorkflowPayload(BotWorkflow BotWorkflow);

public record UpdateBotWorkflowInput(
    Guid WorkflowId, 
    string? NodesDefinition, 
    string? EdgesDefinition, 
    string? LayoutDefinition, 
    int? Version, 
    bool? IsActiveBotWorkflow,
    string? InputParametersDefinition = null,
    string? OutputParametersDefinition = null);

public record UpdateBotWorkflowPayload(BotWorkflow? BotWorkflow);

public record DeleteBotWorkflowInput(Guid WorkflowId);

public record DeleteBotWorkflowPayload(BotWorkflow? BotWorkflow);

public record CopyBotWorkflowInput(Guid SourceWorkflowId);

public record CopyBotWorkflowPayload(BotWorkflow? BotWorkflow);

#endregion
