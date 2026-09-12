using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Polly;
using Polly.Retry;
using ProzorroDataMining.Application.ApplicationContracts;
using ProzorroDataMining.Core.Entities.DTOs;

namespace ProzorroDataMining.Infrastructure.HttpClients;

public class ExternalDataClient : IExternalDataClient
{
    private readonly HttpClient _httpClient; private readonly ILogger _logger;
    public ExternalDataClient(HttpClient httpClient, ILogger<ExternalDataClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    public async Task<IEnumerable<ExternalItemDto>> FetchLatestItemsAsync(CancellationToken cancellationToken = default)
    {
        // Network calls are safe because Polly handles 429 and transient errors in the background pipeline
        var response = await _httpClient.GetAsync("v1/catalog/updates", cancellationToken);
        response.EnsureSuccessStatusCode(); var data = await response.Content.ReadFromJsonAsync<IEnumerable<ExternalItemDto>>(cancellationToken: cancellationToken);
        return data ?? Enumerable.Empty<ExternalItemDto>();
    }
    public async Task<IEnumerable<string>> FetchTenderIdsAsync(
    CancellationToken cancellationToken = default)
    {
        const int limit = 1000;

        var decemberStart = new DateTimeOffset(
            2025, 12, 1, 0, 0, 0, TimeSpan.Zero);

        var januaryStart = new DateTimeOffset(
            2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var tenderIds = new List<string>();

        var url =
            $"https://public-api.prozorro.gov.ua/api/2.5/tenders?opt_fields=id,dateCreated,status" +
            $"&limit={limit}" +
            "&descending=1";

        while (!string.IsNullOrEmpty(url))
        {
            var response = await _httpClient.GetAsync(
                url,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TenderListResponseDto>(
                cancellationToken: cancellationToken);

            if (result == null || result.Data == null || result.Data.Count == 0)
            {
                break;
            }

            foreach (var tender in result.Data)
            {
                if (tender.DateCreated >= decemberStart
                    && tender.DateCreated < januaryStart
                    && string.Equals(
                        tender.Status,
                        "complete",
                        StringComparison.OrdinalIgnoreCase))
                {
                    tenderIds.Add(tender.Id);
                }
            }

            if (result.next_page == null ||
                string.IsNullOrEmpty(result.next_page.Uri))
            {
                break;
            }

            url = result.next_page.Uri;
        }

        return tenderIds;
    }
}