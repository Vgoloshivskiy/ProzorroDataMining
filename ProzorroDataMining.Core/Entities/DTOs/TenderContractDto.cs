using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProzorroDataMining.Core.Entities.DTOs
{
    public class TenderContractDto
    {
        [JsonPropertyName("value")]
        public TenderValueDto Value { get; set; }
    }
}
