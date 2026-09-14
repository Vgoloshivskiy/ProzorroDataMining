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
    IReadOnlyCollection<ProzorroDataMining.Core.Entities.DTOs.TenderListItemDto> tenderIds,
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
    ProzorroDataMining.Core.Entities.DTOs.TenderListItemDto tenderItem,
    SemaphoreSlim semaphore,
    List<TenderImportModel> results,
    TenderMapper mapper,
    CancellationToken cancellationToken)
        {
            try
            {
                try
                {
                    // Before fetching detailed tender, check if the external dateModified matches stored value
                    try
                    {
                        var stored = await (_tenderRepository as ProzorroDataMining.Application.RepositoryContracts.ITenderRepository)
                            .GetTenderDateModifiedAsync(tenderItem.Id, cancellationToken);

                        if (stored.HasValue && tenderItem.DateModified.HasValue && stored.Value == tenderItem.DateModified.Value)
                        {
                            _logger.LogDebug("Tender {TenderId} unchanged (dateModified matches); skipping detail fetch.", tenderItem.Id);
                            return;
                        }
                    }
                    catch
                    {
                        // If date check fails for any reason, fall back to fetching details
                    }

                    var response = await _tenderApiRepository.GetTenderAsync(
                        tenderItem.Id,
                        cancellationToken);

                    var tender = mapper.Map(response);

                    if (tender != null)
                    {
                        // Filter out tenders with CPV code 09310000-5 to avoid inserting unwanted items
                        if (string.Equals(tender.CPVCode, "09310000-5", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation("Skipping tender {TenderId} because CPV code {CPV} is excluded.", tenderItem.Id, tender.CPVCode);
                        }
                        else
                        {
                            lock (results)
                            {
                                results.Add(tender);
                            }
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
                    _logger.LogWarning(ex, "Failed to fetch tender {TenderId}; skipping.", tenderItem.Id);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }
        public async IAsyncEnumerable<IReadOnlyCollection<ProzorroDataMining.Core.Entities.DTOs.TenderListItemDto>> FetchTenderIdBatchesAsync(
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
                $"https://public-api.prozorro.gov.ua/api/2.5/tenders?opt_fields=id,dateCreated,dateModified,status" +
                $"&limit={PageSize}" +
                "&descending=1";

            var batch = new List<ProzorroDataMining.Core.Entities.DTOs.TenderListItemDto>(BatchSize);
            // Track seen external ids during this sync run to avoid duplicates across pages/batches
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

                // Safety: check next_page.Offset so we don't accidentally paginate into the entire historical DB
                // page.next_page.Offset typically contains a decimal representation where the integer part is unix seconds
                try
                {
                    var offsetRaw = page.next_page?.Offset;
                    if (!string.IsNullOrEmpty(offsetRaw))
                    {
                        var dot = offsetRaw.IndexOf('.');
                        var intPart = dot >= 0 ? offsetRaw.Substring(0, dot) : offsetRaw;
                        if (long.TryParse(intPart, out var unixSeconds))
                        {
                            try
                            {
                                var nextPageDate = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
                                if (nextPageDate < search_month_start)
                                {
                                    _logger.LogInformation("Next page offset {Offset} corresponds to {NextPageDate} which is before search_month_start {Start}; stopping pagination to avoid pulling old data.", offsetRaw, nextPageDate, search_month_start);
                                    // stop paginating further
                                    uri = null;
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                // ignore invalid unix seconds
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to validate page.next_page.Offset; continuing pagination.");
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
                        // Normalize and truncate dateModified to UTC seconds to match stored format
                        if (tender.DateModified.HasValue)
                        {
                            var utc = tender.DateModified.Value.ToUniversalTime();
                            tender.DateModified = new DateTimeOffset(utc.DateTime.AddTicks(-(utc.Ticks % TimeSpan.TicksPerSecond)), TimeSpan.Zero);
                        }

                        // Skip if we've already queued this external id during this run
                        if (!seen.Add(tender.Id))
                        {
                            continue;
                        }

                        batch.Add(tender);

                        if (batch.Count >= BatchSize)
                        {
                            yield return batch;

                            batch = new List<ProzorroDataMining.Core.Entities.DTOs.TenderListItemDto>(BatchSize);
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
