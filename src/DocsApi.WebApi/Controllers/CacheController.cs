using DocsApi.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace DocsApi.WebApi.Controllers;

[ApiController]
[Route("api")]
public class CacheController(
    RefreshServiceSpecUseCase refresh,
    ClearCacheUseCase clearCache) : ControllerBase
{
    [HttpPost("services/{id}/refresh")]
    public IActionResult Refresh(string id)
    {
        try
        {
            refresh.Execute(id);
            return Ok(new { message = $"Cache cleared for service '{id}'." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("cache/clear")]
    public IActionResult ClearAll()
    {
        clearCache.Execute();
        return Ok(new { message = "All cache cleared." });
    }
}
