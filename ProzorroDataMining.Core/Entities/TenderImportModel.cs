using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProzorroDataMining.Core.Entities
{
    public class TenderImportModel
    {
        public string ExternalId { get; set; }
        // SHA256 hash of the tender payload used to detect unchanged records and avoid unnecessary updates
        public string DataHash { get; set; }
        // Date when the tender was last modified in the external API
        public DateTimeOffset? DateModified { get; set; }
        // Date when the tender was created in the external API
        public DateTimeOffset? DateCreated { get; set; }
        public string CPVCode { get; set; }
        public string Status { get; set; }
        public string ProcuringEntityName { get; set; }
        public decimal? StartingAmount { get; set; }
        public decimal? ContractTotal { get; set; }
        public decimal? Savings { get; set; }
        public IReadOnlyCollection<string> BusinessOrganisationNames { get; set; }
    }
}
