using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProzorroDataMining.Core.Entities.DTOs
{
    public class TenderValueDto
    {
        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }
    }
}
