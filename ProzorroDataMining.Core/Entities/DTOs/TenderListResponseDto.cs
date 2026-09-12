using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProzorroDataMining.Core.Entities.DTOs
{
    public class TenderListResponseDto
    {
        public List<TenderListItemDto> Data { get; set; }

        public NextPageDto next_page { get; set; }
    }
}
