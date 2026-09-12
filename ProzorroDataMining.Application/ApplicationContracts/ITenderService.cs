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
        IAsyncEnumerable<IReadOnlyCollection<string>> FetchTenderIdBatchesAsync(
            CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<TenderImportModel>> FetchTenderDetailsAsync(
    IReadOnlyCollection<string> tenderIds,
    CancellationToken cancellationToken = default);
    }
}
