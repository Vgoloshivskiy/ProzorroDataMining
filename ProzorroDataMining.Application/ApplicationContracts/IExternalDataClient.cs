using ProzorroDataMining.Core.Entities.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProzorroDataMining.Application.ApplicationContracts
{
    public interface IExternalDataClient
    {
        // Fetches remote items to sync with our database
        Task<IEnumerable<ExternalItemDto>> FetchLatestItemsAsync(CancellationToken cancellationToken = default);
    }
    public interface IItemRepository
    {
        // Placeholder database operations
        Task UpsertItemsAsync(IEnumerable<ExternalItemDto> items, CancellationToken cancellationToken = default);
    }

    public record ExternalItemDto(string Id, string Name, decimal Price, DateTime LastUpdated);
}
