using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BankGateway.Domain.Models.DTO.BaamDTO
{
    /// <param name="OrderId">Id from payment Ordet Register</param>
    /// <param name="Object"></param>
    /// <param name="PacketId">Unique id for each file</param>
    public class BaamBatchTransactionInput
    {
        public Guid OrderId { get; set; }
        public List<TransferInfo> TransferInfos { get; set; }
        public Guid PackageId { get; set; }
    }

    public class TransferInfo
    {
        [JsonProperty(PropertyName = "orderId")]
        public Guid OrderId { get; set; }

        [JsonProperty(PropertyName = "recordId")]
        public Guid RecordId { get; set; }

        [JsonProperty(PropertyName = "destIBAN")]
        public string DestinationIban { get; set; }

        [JsonProperty(PropertyName = "destIBANOwner")]
        public string DestinationIbanName { get; set; }

        [JsonProperty(PropertyName = "amount")]
        public double Amount { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
    }
}
