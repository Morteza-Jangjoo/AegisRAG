using AegisRAG.Application.RAG.Search;
using Microsoft.AspNetCore.Mvc;

namespace AegisRAG.Api.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly SemanticSearchHandler _handler;

    public SearchController(
        SemanticSearchHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
     [FromQuery] string query,
     [FromQuery] int topK = 5,
     [FromQuery] double minimumSimilarity = 0.40,
     CancellationToken cancellationToken = default)
    {
        var result = await _handler.HandleAsync(
            query,
            topK,
            minimumSimilarity,
            cancellationToken);

        return Ok(result);
    }
}