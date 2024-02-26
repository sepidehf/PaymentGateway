using System;

namespace BankGateway.Domain.Models.DTO.BaamDTO
{
   public class RecordInqueryInput
    {
        public Guid? PackageId { get; set; }
        public Guid OrderId { get; set; }
        public Guid RecordId { get; set; } 
       
    }
}
