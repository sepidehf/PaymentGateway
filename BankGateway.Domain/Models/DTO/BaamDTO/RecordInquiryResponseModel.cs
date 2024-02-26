using System;
using System.Collections.Generic;
using BankGateway.Domain.Models.DTO.Results;
using Newtonsoft.Json;

namespace BankGateway.Domain.Models.DTO.BaamDTO
{
    public class RecordInquiryResponseModel : Result
    {
        [JsonProperty(PropertyName = "transfer-info")]
        public List<TransferInfo> TransferInfos { get; set; } 
        //public DateTime ReceivedDateTime { get; set; }//TODO time is NULL
        public string Status { get; set; }
        public string StatusCode { get; set; }
        public Guid RecordId { get; set; }
        [JsonProperty(PropertyName = "destIBAN")]
        public string DestinationIban { get; set; }
        [JsonProperty(PropertyName = "destIBANOwner")]
        public string IbanOwner { get; set; }
        [JsonProperty(PropertyName = "amount")]
        public string Amount { get; set; }
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
        [JsonProperty(PropertyName = "orderId")]
        public Guid OrderId { get; set; }

    }
}
