using AegisRAG.Application.RAG.Chat;
using Microsoft.AspNetCore.Mvc;

namespace AegisRAG.Api.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly RagQueryHandler _handler;

    public ChatController(
        RagQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> Chat(
        [FromBody] RagQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _handler.HandleAsync(
            query,
            cancellationToken);

        return Ok(result);
    }

    
    [HttpPost("stream")]
    public async Task Stream(
    [FromBody] RagQuery query,
    CancellationToken cancellationToken)
    {
        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/plain; charset=utf-8";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        await Response.StartAsync(cancellationToken);

        await foreach (var chunk in
            _handler.StreamAsync(
                query,
                cancellationToken))
        {
            Console.WriteLine(
                $"[API] Writing RAG chunk: '{chunk}'");

            await Response.WriteAsync(
                chunk,
                cancellationToken);

            await Response.Body.FlushAsync(
                cancellationToken);
        }

        Console.WriteLine("[API] RAG stream completed.");
    }
}