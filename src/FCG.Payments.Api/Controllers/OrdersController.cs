using FCG.Payments.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FCG.Payments.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly PaymentsDbContext _db;
    public OrdersController(PaymentsDbContext db) => _db = db;

    /// <summary>Consulta status de uma ordem de pagamento (para debug/observabilidade).</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var o = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return o is null
            ? NotFound(new { message = "Order não encontrada." })
            : Ok(new
            {
                o.Id, o.UserId, o.GameId, o.GameName, o.Amount,
                Status = o.Status.ToString(),
                o.CreatedAt, o.ProcessedAt, o.FailureReason, o.PromocaoId
            });
    }
}
