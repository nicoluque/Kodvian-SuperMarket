using Microsoft.AspNetCore.Mvc;
using KodvianSuperMarket.Data;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public HealthController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<object>> Get()
    {
        var dbOk = await _db.Database.CanConnectAsync();
        if (!dbOk)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "degraded", dbOk = false, at = DateTime.UtcNow });

        return Ok(new { status = "ok", dbOk = true, at = DateTime.UtcNow });
    }
}
