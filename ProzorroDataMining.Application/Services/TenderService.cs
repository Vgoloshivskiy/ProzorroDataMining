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
        private readonly DateTimeOffset _searchMonthStart;
        private readonly DateTimeOffset _searchMonthEnd;

        public TenderService(
            ITenderRepository tenderRepository,
            ITenderApiRepository tenderApiRepository,
            ILogger<TenderService> logger,
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _tenderRepository = tenderRepository;
            _tenderApiRepository = tenderApiRepository;
            _logger = logger;

            // Read date range from configuration (supports keys: DataSync:SearchMonthStart / DataSync:SearchMonthEnd
            // or environment variables DATASYNC_SEARCH_MONTH_START / DATASYNC_SEARCH_MONTH_END).
            DateTimeOffset startFallback, endFallback;
            var now = DateTimeOffset.UtcNow.Date;
            endFallback = now;
            startFallback = now.AddMonths(-1);

            DateTimeOffset parsedStart, parsedEnd;
            var startRaw = configuration["DataSync:SearchMonthStart"] ?? configuration["DATASYNC_SEARCH_MONTH_START"];
            var endRaw = configuration["DataSync:SearchMonthEnd"] ?? configuration["DATASYNC_SEARCH_MONTH_END"];

            if (!string.IsNullOrEmpty(startRaw) && DateTimeOffset.TryParse(startRaw, out parsedStart))
            {
                _searchMonthStart = parsedStart.ToUniversalTime();
            }
            else
            {
                _logger.LogWarning("Invalid or missing DataSync:SearchMonthStart ('{Value}'), falling back to one month ago: {Fallback}", startRaw, startFallback);
                _searchMonthStart = startFallback;
            }

            if (!string.IsNullOrEmpty(endRaw) && DateTimeOffset.TryParse(endRaw, out parsedEnd))
            {
                _searchMonthEnd = parsedEnd.ToUniversalTime();
            }
            else
            {
                _logger.LogWarning("Invalid or missing DataSync:SearchMonthEnd ('{Value}'), falling back to today: {Fallback}", endRaw, endFallback);
                _searchMonthEnd = endFallback;
            }
        }
        /// <summary>
        /// Fetches detailed tender information for a collection of tender IDs, mapping them to TenderImportModel instances.
        /// </summary>
        /// <param name="tenderIds"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Processes a single tender by fetching its details and mapping it to a TenderImportModel.
        /// </summary>
        /// <param name="tenderItem"></param>
        /// <param name="semaphore"></param>
        /// <param name="results"></param>
        /// <param name="mapper"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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
                        //insert only tenders we are interested in
                        if (string.Equals(tender.CPVCode, "09310000-5", StringComparison.OrdinalIgnoreCase))
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
        /// <summary>
        /// Fetches batches of tender IDs from the external API, yielding them as asynchronous enumerable collections. Each batch contains a maximum of BatchSize tender IDs. The method handles pagination and stops fetching when it reaches tenders outside the specified date range or when it detects that the first tender on a page has not changed since the last sync.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<IReadOnlyCollection<ProzorroDataMining.Core.Entities.DTOs.TenderListItemDto>> FetchTenderIdBatchesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var search_month_start = _searchMonthStart;

            var search_month_end = _searchMonthEnd;

            var uri =
                $"https://public-api.prozorro.gov.ua/api/2.5/tenders?opt_fields=id,dateCreated,dateModified,status" +
                $"&limit={PageSize}" +
                "&descending=1";

            var batch = new List<ProzorroDataMining.Core.Entities.DTOs.TenderListItemDto>(BatchSize);
            // Track seen external ids during this sync run to avoid duplicates across pages/batches
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var stopPaging = false;
            while (!string.IsNullOrEmpty(uri) && !stopPaging)
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

                // Check only the first relevant tender on the page to decide whether to stop paginating.
                var first_relevant = page.Data.FirstOrDefault(t =>
                    t.DateCreated >= search_month_start &&
                    t.DateCreated < search_month_end &&
                    string.Equals(t.Status, "complete", StringComparison.OrdinalIgnoreCase));

                if (first_relevant != null)
                {
                    // Normalize and truncate dateModified to UTC seconds to match stored format
                    if (first_relevant.DateModified.HasValue)
                    {
                        var utcFirst = first_relevant.DateModified.Value.ToUniversalTime();
                        first_relevant.DateModified = new DateTimeOffset(utcFirst.DateTime.AddTicks(-(utcFirst.Ticks % TimeSpan.TicksPerSecond)), TimeSpan.Zero);
                    }

                    try
                    {
                        var storedFirst = await _tenderRepository.GetTenderDateModifiedAsync(first_relevant.Id, cancellationToken);
                        if (storedFirst.HasValue && first_relevant.DateModified.HasValue && storedFirst.Value == first_relevant.DateModified.Value)
                        {
                            _logger.LogInformation("First tender on page {TenderId} matches stored dateModified; stopping further pagination.", first_relevant.Id);
                            stopPaging = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to check stored date_modified for first tender on page {TenderId}; will continue.", first_relevant.Id);
                    }
                }

                if (stopPaging)
                {
                    // stop paginating further pages
                    break;
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
