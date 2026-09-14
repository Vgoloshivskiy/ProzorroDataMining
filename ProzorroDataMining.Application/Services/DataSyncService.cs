using ProzorroDataMining.Application.ApplicationContracts;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProzorroDataMining.Application.RepositoryContracts;

namespace ProzorroDataMining.Application.Services
{
    public interface IDataSyncService { Task<bool> RefreshLocalDataAsync(CancellationToken cancellationToken = default); }
    public class DataSyncService : IDataSyncService
    {
        private readonly ITenderRepository _tenderRepository;
        private readonly ILogger _logger;
        private readonly ITenderService _tenderService;
        public DataSyncService(ILogger<DataSyncService> logger, ITenderService tenderService, ITenderRepository tenderRepository)
        {
            _tenderService = tenderService;
            _logger = logger;
            _tenderRepository = tenderRepository;
        }
        public async Task<bool> RefreshLocalDataAsync(
    CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Beginning synchronization of local database with external system...");

            try
            {
                await foreach (var tenderItems in _tenderService
                    .FetchTenderIdBatchesAsync(cancellationToken))
                {
                    var tenders = await _tenderService
                        .FetchTenderDetailsAsync(
                            tenderItems,
                            cancellationToken);

                    await _tenderRepository
                        .UpsertTendersAsync(
                            tenders,
                            cancellationToken);
                }

                _logger.LogInformation(
                    "Synchronization completed successfully.");

                return true;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "Synchronization was cancelled.");

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Data synchronization failed during execution.");

                return false;
            }
        }
    }
}
