using AegisRAG.Application.Documents.GetStatus;
using AegisRAG.Application.Documents.Upload;
using Microsoft.AspNetCore.Mvc;

namespace AegisRAG.Api.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly UploadDocumentHandler _uploadHandler;
    private readonly GetDocumentStatusHandler _statusHandler;

    public DocumentsController(
    UploadDocumentHandler uploadHandler,
    GetDocumentStatusHandler statusHandler)
    {
        _uploadHandler = uploadHandler;
        _statusHandler = statusHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null)
            return BadRequest("File is required.");

        await using var stream = file.OpenReadStream();

        var command = new UploadDocumentCommand(
            file.FileName,
            file.ContentType,
            file.Length,
            stream);

        var result = await _uploadHandler.HandleAsync(
            command,
            cancellationToken);

        return Created(
            $"/api/documents/{result.DocumentId}",
            result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetStatus(
    Guid id,
    CancellationToken cancellationToken)
    {
        var result =
            await _statusHandler.HandleAsync(
                id,
                cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }
}