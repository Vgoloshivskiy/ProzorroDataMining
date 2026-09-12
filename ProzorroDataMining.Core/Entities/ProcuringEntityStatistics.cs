using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProzorroDataMining.Core.Entities
{
    public class ProcuringEntityStatistics
    {
        public int ProcuringEntityId { get; set; }

        public long TenderCount { get; set; }

        public decimal TotalBudget { get; set; }

        public decimal TotalContractValue { get; set; }

        public decimal TotalSavings { get; set; }

        public ProcuringEntity ProcuringEntity { get; set; }
    }
}
