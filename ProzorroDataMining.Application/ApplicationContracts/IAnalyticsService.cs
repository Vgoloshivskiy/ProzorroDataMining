using ProzorroDataMining.Application.RepositoryContracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProzorroDataMining.Application.ApplicationContracts
{
    public interface IAnalyticsService
    {
        /// <summary>
        /// Returns top procuring entities ordered by total contract amount.
        /// </summary>
        /// <param name="top">Number of entities to return.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<IReadOnlyCollection<NameValueDto>> GetTopProcuringEntitiesAsync(int top, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns top suppliers ordered by total contract amount.
        /// </summary>
        /// <param name="top">Number of suppliers to return.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<IReadOnlyCollection<NameValueDto>> GetTopSuppliersAsync(int top, CancellationToken cancellationToken = default);

        /// <summary>
        /// Computes budget savings for a tender identified by external id.
        /// </summary>
        /// <param name="tenderExternalId">Tender external identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<decimal?> GetBudgetSavingsByTenderIdAsync(string tenderExternalId, CancellationToken cancellationToken = default);
    }
}
