using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowService.Api;
using WorkflowService.Services;

namespace WorkflowService.Controllers;

[ApiController]
public class WorkflowAiController : ControllerBase
{
    [HttpGet("/public/workflow-ai/prompt")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPrompt([FromServices] WorkflowAiPromptProvider prompts, CancellationToken ct)
    {
        var md = await prompts.GetMarkdownAsync(ct);
        var version = WorkflowAiPromptProvider.ComputeEtag(md);
        Response.Headers.ETag = $"\"{version}\"";
        Response.Headers.Append("X-Prompt-Version", version);
        Response.Headers.CacheControl = "public, max-age=3600";
        return Content(md, "text/markdown; charset=utf-8");
    }

    [HttpPost("/api/workflow-ai/generate")]
    [Authorize]
    public async Task<IActionResult> Generate(
        [FromBody] GenerateWorkflowFromAiHttpRequest? body,
        [FromServices] WorkflowAiGenerationService gen,
        CancellationToken ct)
    {
        if (body is null)
            return BadRequest();

        var result = await gen.GenerateAsync(body.UserPrompt, body.Mode ?? "replace", body.CurrentWorkflow, ct);
        return new JsonResult(
            new
            {
                success = result.Success,
                workflowJson = result.WorkflowJson,
                errors = result.Errors
            },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}
