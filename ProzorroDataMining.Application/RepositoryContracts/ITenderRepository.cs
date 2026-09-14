using ProzorroDataMining.Core.Entities;
using ProzorroDataMining.Core.Entities.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProzorroDataMining.Application.RepositoryContracts
{
    public interface ITenderRepository
    {
        Task UpsertTendersAsync(
            IReadOnlyCollection<TenderImportModel> tenders,
            CancellationToken cancellationToken = default);

        // Returns the stored date_modified for a tender or null if the tender does not exist
        Task<DateTimeOffset?> GetTenderDateModifiedAsync(string externalId, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<TenderListItemDto>> GetTenderListAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    }
}
