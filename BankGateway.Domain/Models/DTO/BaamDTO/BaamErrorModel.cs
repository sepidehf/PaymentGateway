using System.Collections.Generic;

namespace BankGateway.Domain.Models.DTO.BaamDTO
{
    public class BaamErrorModel
    {
        public string ErrorCode { get; set; }
        public string ErrorSummary { get; set; }
        public List<Causes> ErrorCauses { get; set; }

    }

    public class Causes
    {
        public string Cause { get; set; }
    }
}