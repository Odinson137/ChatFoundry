using System.Security.Claims;
using FileService.Entities;
using FileService.Interfaces;
using FileService.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Controllers;

[ApiController]
[Route("files")]
public class FilesController(
    IStorageService storage,
    IFileRepository fileRepository,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
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

        var (userId, companyIdFromClaim) = GetCurrentUserAndCompany();

        await using var stream = file.OpenReadStream();
        var (key, url) = await storage.UploadAsync(stream, file.FileName, file.ContentType, ct);

        var user = httpContextAccessor.HttpContext?.User;
        var entity = new FileEntity
        {
            UploadedByUserId = userId != Guid.Empty ? userId : null,
            UploadedByUserName = UploaderDisplayNameHelper.FromPrincipal(user),
            CompanyId = companyId ?? companyIdFromClaim ?? Guid.Empty,
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
    public async Task<IActionResult> GetUrl([FromQuery] string? key, [FromQuery] bool download = false, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return BadRequest("key is required");

        string? downloadFileName = null;
        if (download)
        {
            var file = await fileRepository.GetByKeyAsync(key.Trim(), ct);
            downloadFileName = file?.OriginalFileName;
        }

        var url = await storage.GetSignedUrlAsync(key.Trim(), downloadFileName: downloadFileName, ct: ct);
        if (url == null)
            return NotFound();

        return Ok(new { url });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var file = await fileRepository.GetByIdAsync(id, ct);
        if (file == null)
            return NotFound();

        await storage.DeleteAsync(file.Key, ct);
        await fileRepository.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct = default)
    {
        var file = await fileRepository.GetByIdAsync(id, ct);
        if (file == null)
            return NotFound();

        var url = await storage.GetSignedUrlAsync(file.Key, downloadFileName: file.OriginalFileName, ct: ct);
        if (string.IsNullOrEmpty(url))
            return NotFound();
        return Redirect(url);
    }

    private (Guid UserId, Guid? CompanyId) GetCurrentUserAndCompany()
    {
        var user = httpContextAccessor.HttpContext?.User;
        var userIdClaim = user?.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? user?.FindFirstValue("sub");
        var userId = Guid.TryParse(userIdClaim, out var uid) ? uid : Guid.Empty;
        var companyIdClaim = user?.FindFirstValue("company_id");
        var companyId = Guid.TryParse(companyIdClaim, out var cid) ? cid : (Guid?)null;
        return (userId, companyId);
    }
}
