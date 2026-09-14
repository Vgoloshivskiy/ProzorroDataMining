using ProzorroDataMining.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProzorroDataMining.Application.ApplicationContracts
{
    public interface ITenderService
    {
        IAsyncEnumerable<IReadOnlyCollection<ProzorroDataMining.Core.Entities.DTOs.TenderListItemDto>> FetchTenderIdBatchesAsync(
            CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<TenderImportModel>> FetchTenderDetailsAsync(
    IReadOnlyCollection<ProzorroDataMining.Core.Entities.DTOs.TenderListItemDto> tenderIds,
    CancellationToken cancellationToken = default);
    }
}
