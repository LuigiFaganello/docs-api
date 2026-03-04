using DocsApi.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace DocsApi.WebApi.Controllers;

[ApiController]
[Route("api/services")]
public class ServicesController(
    GetServiceListUseCase getList,
    GetServiceSpecUseCase getSpec) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll() => Ok(getList.Execute());

    [HttpGet("{id}/spec")]
    public async Task<IActionResult> GetSpec(string id, CancellationToken cancellationToken)
    {
        SpecResult result;
        try
        {
            result = await getSpec.ExecuteAsync(id, cancellationToken);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { error = "Failed to fetch spec.", detail = ex.Message, service = id });
        }

        Response.Headers["X-Cache"] = result.CacheStatus.ToString().ToUpperInvariant();
        return Content(result.Json, "application/json");
    }
}
