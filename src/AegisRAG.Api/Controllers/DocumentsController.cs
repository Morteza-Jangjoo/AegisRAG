using AegisRAG.Application.Documents.Upload;
using Microsoft.AspNetCore.Mvc;

namespace AegisRAG.Api.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly UploadDocumentHandler _handler;

    public DocumentsController(
        UploadDocumentHandler handler)
    {
        _handler = handler;
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

        var result = await _handler.HandleAsync(
            command,
            cancellationToken);

        return Created(
            $"/api/documents/{result.DocumentId}",
            result);
    }
}