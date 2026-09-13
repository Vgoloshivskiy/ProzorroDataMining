using Microsoft.AspNetCore.Mvc;
using ProzorroDataMining.Application.RepositoryContracts;

namespace ProzorroDataMining.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly ITenderAnalyticsRepository _analytics;

        public AnalyticsController(ITenderAnalyticsRepository analytics)
        {
            _analytics = analytics;
        }
        // Summary endpoint removed. Use the dedicated endpoints below.

        [HttpGet("tender/{externalId}/savings")]
        public async Task<IActionResult> GetTenderSavings(string externalId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(externalId))
                return BadRequest(new { Message = "Tender external id is required." });

            var savings = await _analytics.GetBudgetSavingsByTenderIdAsync(externalId, cancellationToken);

            if (!savings.HasValue)
                return NotFound(new { Message = "Tender not found.", ExternalId = externalId });

            return Ok(new { ExternalId = externalId, BudgetSavings = savings.Value });
        }

        [HttpGet("top/procuring-entities")]
        public async Task<IActionResult> GetTopProcuringEntities([FromQuery] int top = 5, CancellationToken cancellationToken = default)
        {
            var rows = await _analytics.GetTopProcuringEntitiesAsync(top, cancellationToken);
            return Ok(rows);
        }

        [HttpGet("top/suppliers")]
        public async Task<IActionResult> GetTopSuppliers([FromQuery] int top = 5, CancellationToken cancellationToken = default)
        {
            var rows = await _analytics.GetTopSuppliersAsync(top, cancellationToken);
            return Ok(rows);
        }
    }
}
