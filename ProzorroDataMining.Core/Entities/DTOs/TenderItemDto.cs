using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProzorroDataMining.Core.Entities.DTOs
{
    public class TenderItemDto
    {
        [JsonPropertyName("classification")]
        public ClassificationDto Classification { get; set; }
    }

    public class ClassificationDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }
}
