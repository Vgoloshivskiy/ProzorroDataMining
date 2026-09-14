using ProzorroDataMining.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProzorroDataMining.Application.ApplicationContracts
{
    public interface IDataSyncService { Task<bool> RefreshLocalDataAsync(CancellationToken cancellationToken = default); }
}
