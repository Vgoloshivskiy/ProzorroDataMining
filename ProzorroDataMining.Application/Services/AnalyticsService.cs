using ProzorroDataMining.Application.ApplicationContracts;
using ProzorroDataMining.Application.RepositoryContracts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProzorroDataMining.Application.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly ITenderAnalyticsRepository _analyticsRepository;

        /// <summary>
        /// Constructs an AnalyticsService that delegates to the analytics repository.
        /// </summary>
        /// <param name="analyticsRepository">Repository implementing analytics queries.</param>
        public AnalyticsService(ITenderAnalyticsRepository analyticsRepository)
        {
            _analyticsRepository = analyticsRepository;
        }
        /// <summary>
        /// Gets the top procuring entities based on the number of tenders they have issued.
        /// </summary>
        /// <param name="top"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<IReadOnlyCollection<NameValueDto>> GetTopProcuringEntitiesAsync(int top, CancellationToken cancellationToken = default)
        {
            return _analyticsRepository.GetTopProcuringEntitiesAsync(top, cancellationToken);
        }
        /// <summary>
        /// Gets the top suppliers based on the number of tenders they have won.
        /// </summary>
        /// <param name="top"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<IReadOnlyCollection<NameValueDto>> GetTopSuppliersAsync(int top, CancellationToken cancellationToken = default)
        {
            return _analyticsRepository.GetTopSuppliersAsync(top, cancellationToken);
        }
        /// <summary>
        /// Computes the budget savings for a tender identified by its external id.
        /// </summary>
        /// <param name="tenderExternalId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<decimal?> GetBudgetSavingsByTenderIdAsync(string tenderExternalId, CancellationToken cancellationToken = default)
        {
            return _analyticsRepository.GetBudgetSavingsByTenderIdAsync(tenderExternalId, cancellationToken);
        }
    }
}
