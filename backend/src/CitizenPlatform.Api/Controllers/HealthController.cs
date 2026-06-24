using CitizenPlatform.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace CitizenPlatform.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<object>> Get()
    {
        var payload = new
        {
            Service = "CitizenPlatform.Api",
            Status = "Healthy",
            CheckedAt = DateTimeOffset.UtcNow
        };

        return Ok(ApiResponse<object>.Ok(payload));
    }
}
