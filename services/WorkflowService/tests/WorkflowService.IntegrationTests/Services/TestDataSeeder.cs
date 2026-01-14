using Microsoft.Extensions.DependencyInjection;
using WorkflowService.Entities;
using WorkflowService.Interfaces;

namespace WorkflowService.IntegrationTests.Services;

public static class TestDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var workflowRepo = services.GetRequiredService<IWorkflowRepository>();

        await workflowRepo.SaveAsync(new BotWorkflow
        {
            Bot = new Bot { Name = "Test" },
            Version = 1,
            NodesDefinition = TestWorkflowSchemas.NodesJson,
            EdgesDefinition = TestWorkflowSchemas.EdgesJson,
            LayoutDefinition = TestWorkflowSchemas.LayoutJson,
        });
    }
}

public static class TestWorkflowSchemas
{
    public static string NodesJson =>
        """
        [
          {
            "id": "11111111-1111-1111-1111-111111111111",
            "type": "Start",
            "label": "Start",
            "data": {}
          },
          {
            "id": "22222222-2222-2222-2222-222222222222",
            "type": "Ask",
            "label": "Ask user",
            "data": {
              "text": "Choose option",
              "buttons": [
                { "id": "order", "text": "Order" },
                { "id": "faq", "text": "FAQ" }
              ],
              "variable": "user_choice"
            }
          },
          {
            "id": "33333333-3333-3333-3333-333333333333",
            "type": "Message",
            "label": "Order selected",
            "data": {
              "text": "You selected {{user_choice}}"
            }
          }
        ]
        """;

    public static string EdgesJson =>
        """
        [
          { 
            "from": "11111111-1111-1111-1111-111111111111", 
            "to": "22222222-2222-2222-2222-222222222222" 
          },
          {
            "from": "22222222-2222-2222-2222-222222222222",
            "to": "33333333-3333-3333-3333-333333333333",
            "condition": {
              "equals": {
                "left": "$user_choice",
                "right": "order"
              }
            }
          }
        ]
        """;

    public static string LayoutJson =>
        """
        [
          { "nodeId": "11111111-1111-1111-1111-111111111111", "x": 100, "y": 100 },
          { "nodeId": "22222222-2222-2222-2222-222222222222", "x": 100, "y": 250 },
          { "nodeId": "33333333-3333-3333-3333-333333333333", "x": 100, "y": 450 }
        ]
        """;
}