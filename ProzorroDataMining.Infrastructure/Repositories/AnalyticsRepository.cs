using Dapper;
using ProzorroDataMining.Application.RepositoryContracts;
using ProzorroDataMining.Infrastructure.DbContext;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProzorroDataMining.Infrastructure.Repositories
{
    public class AnalyticsRepository : ITenderAnalyticsRepository
    {
        private readonly DapperDbContext _dbContext;

        public AnalyticsRepository(DapperDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        // Global budget savings removed; keep per-tender savings only.

        public async Task<IReadOnlyCollection<NameValueDto>> GetTopProcuringEntitiesAsync(int top, CancellationToken cancellationToken = default)
        {
            var sql = @"
SELECT pe.name AS Name, COALESCE(SUM(t.contract_total),0) AS Total
FROM tender t
JOIN procuring_entity pe ON pe.id = t.procuring_entity_id
GROUP BY pe.name
ORDER BY Total DESC
LIMIT @Top;";

            using var conn = _dbContext.DbConnection;
            await conn.OpenAsync(cancellationToken);

            var rows = await conn.QueryAsync<NameValueDto>(sql, new { Top = top });
            return rows.ToList();
        }

        public async Task<IReadOnlyCollection<NameValueDto>> GetTopSuppliersAsync(int top, CancellationToken cancellationToken = default)
        {
            var sql = @"
SELECT bo.name AS Name, COALESCE(SUM(t.contract_total),0) AS Total
FROM tender_business_organisation tbo
JOIN business_organisation bo ON bo.id = tbo.business_organisation_id
JOIN tender t ON t.id = tbo.tender_id
GROUP BY bo.name
ORDER BY Total DESC
LIMIT @Top;";

            using var conn = _dbContext.DbConnection;
            await conn.OpenAsync(cancellationToken);

            var rows = await conn.QueryAsync<NameValueDto>(sql, new { Top = top });
            return rows.ToList();
        }

        public async Task<decimal?> GetBudgetSavingsByTenderIdAsync(string tenderExternalId, CancellationToken cancellationToken = default)
        {
            var sql = @"SELECT (COALESCE(starting_amount,0) - COALESCE(contract_total,0))::numeric FROM tender WHERE external_id = @ExternalId LIMIT 1;";

            using var conn = _dbContext.DbConnection;
            await conn.OpenAsync(cancellationToken);

            var result = await conn.QuerySingleOrDefaultAsync<decimal?>(sql, new { ExternalId = tenderExternalId });
            return result;
        }
    }
}
