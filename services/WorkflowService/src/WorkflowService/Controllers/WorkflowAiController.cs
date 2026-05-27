using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowService.Models;
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
        [FromServices] BillingQuotaGuard billing,
        CancellationToken ct)
    {
        if (body is null)
            return BadRequest();

        var companyClaim = User.FindFirstValue("company_id");
        Guid? companyId = Guid.TryParse(companyClaim, out var cid) ? cid : null;

        try
        {
            await billing.EnsureQuotaAsync(companyId, "ai_tokens", 0, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, errors = new[] { ex.Message } });
        }

        var result = await gen.GenerateAsync(body.UserPrompt, body.Mode ?? "replace", body.CurrentWorkflow, ct);

        if (result.Success && companyId.HasValue && result.TokensUsed > 0)
            await billing.IncrementUsageAsync(companyId, "ai_tokens", result.TokensUsed, ct);

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
