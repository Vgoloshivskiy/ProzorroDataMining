using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProzorroDataMining.Core.Entities.DTOs
{
    public class TenderAwardDto
    {
        [JsonPropertyName("suppliers")]
        public List<TenderSupplierDto> Suppliers { get; set; }
    }

    public class TenderSupplierDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
