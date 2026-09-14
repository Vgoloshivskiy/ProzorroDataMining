using Microsoft.AspNetCore.Mvc;
using ProzorroDataMining.Application.RepositoryContracts;

namespace ProzorroDataMining.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    /// <summary>
    /// Controller exposing analytics endpoints such as top suppliers, top procuring entities and per-tender savings.
    /// </summary>
    public class AnalyticsController : ControllerBase
    {
        private readonly ProzorroDataMining.Application.ApplicationContracts.IAnalyticsService _analyticsService;

        /// <summary>
        /// Creates a new <see cref="AnalyticsController"/>.
        /// </summary>
        /// <param name="analyticsService">Application-level analytics service.</param>
        public AnalyticsController(ProzorroDataMining.Application.ApplicationContracts.IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        // Summary endpoint removed. Use the dedicated endpoints below.

        /// <summary>
        /// Returns budget savings for a single tender identified by externalId.
        /// </summary>
        [HttpGet("tender/{externalId}/savings")]
        public async Task<IActionResult> GetTenderSavings(string externalId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(externalId))
                return BadRequest(new { Message = "Tender external id is required." });

            var savings = await _analyticsService.GetBudgetSavingsByTenderIdAsync(externalId, cancellationToken);

            if (!savings.HasValue)
                return NotFound(new { Message = "Tender not found.", ExternalId = externalId });

            return Ok(new { ExternalId = externalId, BudgetSavings = savings.Value });
        }

        /// <summary>
        /// Returns top procuring entities ordered by total contract amount.
        /// </summary>
        [HttpGet("top/procuring-entities")]
        public async Task<IActionResult> GetTopProcuringEntities([FromQuery] int top = 5, CancellationToken cancellationToken = default)
        {
            var rows = await _analyticsService.GetTopProcuringEntitiesAsync(top, cancellationToken);
            return Ok(rows);
        }

        /// <summary>
        /// Returns top suppliers ordered by total contract amount.
        /// </summary>
        [HttpGet("top/suppliers")]
        public async Task<IActionResult> GetTopSuppliers([FromQuery] int top = 5, CancellationToken cancellationToken = default)
        {
            var rows = await _analyticsService.GetTopSuppliersAsync(top, cancellationToken);
            return Ok(rows);
        }
    }
}
