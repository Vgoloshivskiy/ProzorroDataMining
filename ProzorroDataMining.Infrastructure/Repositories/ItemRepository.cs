using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProzorroDataMining.Application.ApplicationContracts;
using ProzorroDataMining.Core.Entities.DTOs;
using ProzorroDataMining.Infrastructure.DbContext;

namespace ProzorroDataMining.Infrastructure.Repositories
{
    public class ItemRepository : IItemRepository
    {
        private readonly DapperDbContext _db;
        public ItemRepository(DapperDbContext db)
        {
            _db = db;
        }

        public Task UpsertItemsAsync(IEnumerable<ExternalItemDto> items, CancellationToken cancellationToken = default)
        {
            // Provide a simple no-op or implement upsert logic here.
            // No-op to satisfy DI and allow the app to start; replace with real logic later.
            return Task.CompletedTask;
        }
    }
}