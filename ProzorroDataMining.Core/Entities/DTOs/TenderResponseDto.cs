using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProzorroDataMining.Core.Entities.DTOs
{
    public class TenderResponseDto
    {
        [JsonPropertyName("data")]
        public TenderDataDto Data { get; set; }
    }
}
