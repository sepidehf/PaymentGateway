using System.Collections.Generic;
using BankGateway.Domain.Models.Enum;
using Core.Infrastructure.Common;
using Newtonsoft.Json;
using Result = BankGateway.Domain.Models.DTO.Results.Result;

namespace BankGateway.Domain.Models.DTO.BaamDTO
{
    public class TransactionsInquiryOutput : Result
    {
        public double TotalAmount { get; set; }
        public int TotalCount { get; set; }
        public string Status { get; set; }
        public string StatusCode { get; set; }
        [JsonProperty(PropertyName = "items")]
        public List<SubTransaction> SubTransactions { get; set; } 
    }
}
