using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProzorroDataMining.Core.Entities
{
    public class BusinessOrganisationStatistics
    {
        public int BusinessOrganisationId { get; set; }

        public long ContractCount { get; set; }

        public decimal TotalContractValue { get; set; }

        public BusinessOrganisation BusinessOrganisation { get; set; }
    }
}
