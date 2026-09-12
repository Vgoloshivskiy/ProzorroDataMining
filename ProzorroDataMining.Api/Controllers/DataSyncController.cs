using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProzorroDataMining.Application.ApplicationContracts;
using ProzorroDataMining.Application.Services;
using static System.Net.Mime.MediaTypeNames;

namespace ProzorroDataMining.Api.Controllers
{
    [EnableRateLimiting("PerClient")]
    [ApiController]
    [Route("api/[controller]")]
    public class DataSyncController : ControllerBase
    {
        private readonly IDataSyncService _dataSyncService;
        private readonly SemaphoreSlim _refreshLock;
        public DataSyncController(IDataSyncService dataSyncService, SemaphoreSlim refreshLock)
        {
            _dataSyncService = dataSyncService;
            _refreshLock = refreshLock;
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshDatabase(
    CancellationToken cancellationToken)
        {
            if (!await _refreshLock.WaitAsync(0, cancellationToken))
            {
                return Conflict(new
                {
                    Message = "Database refresh is already in progress."
                });
            }

            try
            {
                var success = await _dataSyncService.RefreshLocalDataAsync(cancellationToken);

                if (!success)
                {
                    return StatusCode(500, new
                    {
                        Message = "Sync failed. Downstream external server may be down or heavily rate-limited."
                    });
                }

                return Ok(new
                {
                    Message = "Local database updated successfully with latest external catalog items."
                });
            }
            finally
            {
                _refreshLock.Release();
            }
        }
    }
}
