using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProzorroDataMining.Core.Entities.DTOs
{
    public class TenderDataDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("value")]
        public TenderValueDto Value { get; set; }

        [JsonPropertyName("procuringEntity")]
        public ProcuringEntityDto ProcuringEntity { get; set; }

        [JsonPropertyName("items")]
        public List<TenderItemDto> Items { get; set; }

        [JsonPropertyName("contracts")]
        public List<TenderContractDto> Contracts { get; set; }

        [JsonPropertyName("awards")]
        public List<TenderAwardDto> Awards { get; set; }
    }
}
