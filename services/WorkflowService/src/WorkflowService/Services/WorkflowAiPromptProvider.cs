using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;

namespace WorkflowService.Services;

public class WorkflowAiPromptProvider(IWebHostEnvironment env, ILogger<WorkflowAiPromptProvider> logger)
{
    private readonly string _path = Path.Combine(env.ContentRootPath, "Resources", "WorkflowAiInstruction.md");

    public async Task<string> GetMarkdownAsync(CancellationToken cancellationToken = default)
    {
        if (!System.IO.File.Exists(_path))
        {
            logger.LogWarning("WorkflowAiInstruction.md not found at {Path}", _path);
            return "# Инструкция\n\nФайл Resources/WorkflowAiInstruction.md не найден на сервере.";
        }

        return await System.IO.File.ReadAllTextAsync(_path, cancellationToken);
    }

    public static string ComputeEtag(string content)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash)[..16];
    }
}
