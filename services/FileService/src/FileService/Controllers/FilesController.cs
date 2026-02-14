using FileService.Entities;
using FileService.Interfaces;
using FileService.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Controllers;

[ApiController]
[Route("files")]
public class FilesController(
    IStorageService storage,
    IFileRepository fileRepository) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        IFormFile? file,
        [FromForm] Guid? companyId,
        [FromForm] Guid? uploadedClientId,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file or empty file");

        await using var stream = file.OpenReadStream();
        var (key, url) = await storage.UploadAsync(stream, file.FileName, file.ContentType, ct);

        var entity = new FileEntity
        {
            UploadedByUserId = Guid.Empty,
            CompanyId = companyId ?? Guid.Empty,
            UploadedClientId = uploadedClientId,
            Key = key,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            Size = file.Length
        };
        await fileRepository.AddAsync(entity, ct);

        return Ok(new { id = entity.Id, key = entity.Key, url });
    }

    [HttpGet("url")]
    public async Task<IActionResult> GetUrl([FromQuery] string? key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return BadRequest("key is required");

        var url = await storage.GetSignedUrlAsync(key.Trim(), null, ct);
        if (url == null)
            return NotFound();

        return Ok(new { url });
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct = default)
    {
        var file = await fileRepository.GetByIdAsync(id, ct);
        if (file == null)
            return NotFound();

        var url = await storage.GetSignedUrlAsync(file.Key, null, ct);
        if (string.IsNullOrEmpty(url))
            return NotFound();
        return Redirect(url);
    }
}
