using System;
using System.Collections.Generic;

namespace BankGateway.Domain.Models.DTO
{
    public class PackageInfoDto
    {
        public Guid PackageId { get; set; }
        public DateTime DateTime { get; set; }
        public List<RecordDto> Records { get; set; } 
    }
}
