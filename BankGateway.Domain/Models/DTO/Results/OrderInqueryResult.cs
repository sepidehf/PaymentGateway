using System.Collections.Generic;
using BankGateway.Domain.Models.DTO.BaamDTO;
using BankGateway.Domain.Models.Enum;
using Core.Infrastructure.Common;

namespace BankGateway.Domain.Models.DTO.Results
{
    public class OrderInquiryResult : Result
    {
        public double TotalAmount { get; set; }
        public int TotalCount { get; set; }
        public CasStatuse Status { get; set; }
        public string StatusCode { get; set; }
        public List<OrderSubTransaction> SubTransactions { get; set; }


    }
}
