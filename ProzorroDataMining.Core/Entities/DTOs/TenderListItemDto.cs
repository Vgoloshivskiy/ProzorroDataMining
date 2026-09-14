using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProzorroDataMining.Core.Entities.DTOs
{
    public class TenderListItemDto
    {
        public string Id { get; set; }

        public DateTimeOffset DateCreated { get; set; }

        // Date the tender was last modified according to the external API
        public DateTimeOffset? DateModified { get; set; }

        public string Status { get; set; }
    }
}
