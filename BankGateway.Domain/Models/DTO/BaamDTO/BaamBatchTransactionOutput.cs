using System;
using BankGateway.Domain.Models.DTO.Results;
using Newtonsoft.Json;

namespace BankGateway.Domain.Models.DTO.BaamDTO
{
    /// <summary>
    /// Amount: SumAmount
    /// </summary>
   public class BaamBatchTransactionOutput : Result
    {
        [JsonProperty(PropertyName = "totalAmount")]
        public double Amount { get; set; }
        public int RecordCount { get; set; }
        public Guid DetailId { get; set; }
        public Guid OrderId { get; set; }
        public string Status { get; set; }
        public string StatusCode { get; set; }
    }
}
