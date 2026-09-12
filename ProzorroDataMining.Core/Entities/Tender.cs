using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProzorroDataMining.Core.Entities
{
    public class Tender
    {
        public long Id { get; set; }
        public string ExternalId { get; set; }
        public string CPVCode { get; set; }
        public string Status { get; set; }
        public int? ProcuringEntityId { get; set; }
        public decimal? StartingAmount { get; set; }
        public decimal? ContractTotal { get; set; }
        public decimal? Savings { get; set; }
    }
}
