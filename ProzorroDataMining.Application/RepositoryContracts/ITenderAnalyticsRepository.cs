using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProzorroDataMining.Application.RepositoryContracts
{
    public interface ITenderAnalyticsRepository
    {
        // Global budget savings removed; per-tender savings endpoint remains
        Task<decimal?> GetBudgetSavingsByTenderIdAsync(string tenderExternalId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<NameValueDto>> GetTopProcuringEntitiesAsync(int top, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<NameValueDto>> GetTopSuppliersAsync(int top, CancellationToken cancellationToken = default);
    }
}
