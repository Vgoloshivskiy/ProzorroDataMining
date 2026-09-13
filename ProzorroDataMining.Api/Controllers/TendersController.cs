using Microsoft.AspNetCore.Mvc;
using ProzorroDataMining.Application.RepositoryContracts;

namespace ProzorroDataMining.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TendersController : ControllerBase
    {
        private readonly ITenderRepository _tenderRepository;

        public TendersController(ITenderRepository tenderRepository)
        {
            _tenderRepository = tenderRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetTenders([FromQuery] int page = 1, [FromQuery] int pageSize = 100, CancellationToken cancellationToken = default)
        {
            if (pageSize > 100) pageSize = 100; // cap

            var items = await _tenderRepository.GetTenderListAsync(page, pageSize, cancellationToken);
            return Ok(new { Page = page, PageSize = pageSize, Items = items });
        }
    }
}
