using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProzorroDataMining.Core.Entities.DTOs;
using ProzorroDataMining.Application.RepositoryContracts;
using System.Net.Http.Json;
using ProzorroDataMining.Infrastructure.Utilities;

namespace ProzorroDataMining.Infrastructure.Repositories
{
    public class TenderApiRepository : ITenderApiRepository
    {
        private readonly HttpClient _httpClient;

        public TenderApiRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<TenderListResponseDto> GetTenderPageAsync(
            string uri,
            CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync(uri, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TenderListResponseDto>(cancellationToken: cancellationToken);
            return result!;
        }

        public async Task<TenderResponseDto> GetTenderAsync(
            string tenderId,
            CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync("tenders/" + tenderId, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TenderResponseDto>(cancellationToken: cancellationToken);
            return result!;
        }
    }
}
