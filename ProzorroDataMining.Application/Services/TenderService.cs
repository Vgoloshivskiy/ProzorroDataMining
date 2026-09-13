using Microsoft.Extensions.Logging;
using ProzorroDataMining.Application.ApplicationContracts;
using ProzorroDataMining.Core.Entities;
using ProzorroDataMining.Core.Mapper;
using ProzorroDataMining.Application.RepositoryContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ProzorroDataMining.Application.Services
{
    public class TenderService : ITenderService
    {
        private const int PageSize = 1000;
        private const int BatchSize = 1000;

        private readonly ITenderRepository _tenderRepository;
        private readonly ITenderApiRepository _tenderApiRepository;
        private readonly ILogger<TenderService> _logger;

        public TenderService(
            ITenderRepository tenderRepository,
            ITenderApiRepository tenderApiRepository,
            ILogger<TenderService> logger)
        {
            _tenderRepository = tenderRepository;
            _tenderApiRepository = tenderApiRepository;
            _logger = logger;
        }
        public async Task<IReadOnlyCollection<TenderImportModel>> FetchTenderDetailsAsync(
    IReadOnlyCollection<string> tenderIds,
    CancellationToken cancellationToken = default)
        {
            if (tenderIds == null || tenderIds.Count == 0)
            {
                return Array.Empty<TenderImportModel>();
            }

            const int maxConcurrency = 10;

            var semaphore = new SemaphoreSlim(maxConcurrency);
            var results = new List<TenderImportModel>(tenderIds.Count);
            var mapper = new TenderMapper();

            var tasks = new List<Task>();

            foreach (var tenderId in tenderIds)
            {
                await semaphore.WaitAsync(cancellationToken);

                tasks.Add(ProcessTenderAsync(
                    tenderId,
                    semaphore,
                    results,
                    mapper,
                    cancellationToken));
            }

            await Task.WhenAll(tasks);

            return results;
        }
        private async Task ProcessTenderAsync(
    string tenderId,
    SemaphoreSlim semaphore,
    List<TenderImportModel> results,
    TenderMapper mapper,
    CancellationToken cancellationToken)
        {
            try
            {
                try
                {
                    var response = await _tenderApiRepository.GetTenderAsync(
                        tenderId,
                        cancellationToken);

                    var tender = mapper.Map(response);

                    if (tender != null)
                    {
                        lock (results)
                        {
                            results.Add(tender);
                        }
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // propagate cancellation
                    throw;
                }
                catch (Exception ex)
                {
                    // Log and continue; we don't want a single failing tender to break the whole batch
                    _logger.LogWarning(ex, "Failed to fetch tender {TenderId}; skipping.", tenderId);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }
        public async IAsyncEnumerable<IReadOnlyCollection<string>> FetchTenderIdBatchesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var search_month_start = new DateTimeOffset(
                2026,
                8,
                1,
                0,
                0,
                0,
                TimeSpan.Zero);

            var search_month_end = new DateTimeOffset(
                2026,
                9,
                1,
                0,
                0,
                0,
                TimeSpan.Zero);

            var uri =
                $"https://public-api.prozorro.gov.ua/api/2.5/tenders?opt_fields=id,dateCreated,status" +
                $"&limit={PageSize}" +
                "&descending=1";

            var batch = new List<string>(BatchSize);

            while (!string.IsNullOrEmpty(uri))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var page = await _tenderApiRepository.GetTenderPageAsync(
                    uri,
                    cancellationToken);

                if (page == null || page.Data == null)
                {
                    yield break;
                }

                foreach (var tender in page.Data)
                {
                    if (tender.DateCreated >= search_month_start &&
                        tender.DateCreated < search_month_end &&
                        string.Equals(
                            tender.Status,
                            "complete",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        batch.Add(tender.Id);

                        if (batch.Count >= BatchSize)
                        {
                            yield return batch;

                            batch = new List<string>(BatchSize);
                        }
                    }
                }

                uri = page.next_page.Uri;
            }

            if (batch.Count > 0)
            {
                yield return batch;
            }
        }
    }
}
