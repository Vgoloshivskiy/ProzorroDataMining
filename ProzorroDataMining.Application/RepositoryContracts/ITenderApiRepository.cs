using ProzorroDataMining.Core.Entities.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProzorroDataMining.Application.RepositoryContracts
{
    public interface ITenderApiRepository
    {
        Task<TenderListResponseDto> GetTenderPageAsync(
            string uri,
            CancellationToken cancellationToken = default);

        Task<TenderResponseDto> GetTenderAsync(
            string tenderId,
            CancellationToken cancellationToken = default);
    }
}
