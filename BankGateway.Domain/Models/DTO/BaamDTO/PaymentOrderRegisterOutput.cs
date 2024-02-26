using BankGateway.Domain.Models.Enum;
using Core.Infrastructure.Common;
using Newtonsoft.Json;
using Result = BankGateway.Domain.Models.DTO.Results.Result;

namespace BankGateway.Domain.Models.DTO.BaamDTO
{
    public class PaymentOrderRegisterOutput : Result
    {
        [JsonProperty(PropertyName = "orderId")]
        public string OrderId { get; set; }

        [JsonProperty(PropertyName = "totalAmount")]
        public double TotalAmount { get; set; }

        [JsonProperty(PropertyName = "recordsCount")]
        public int RecordsCount { get; set; }
        /// <summary>
        /// نام صاحب حساب مبدا(سپرده گذاری همراه با استعلام)
        /// </summary>
        /// <value>The source title.</value>
        [JsonProperty(PropertyName = "srcTitle")]
        public string SrcTitle { get; set; }

        [JsonProperty(PropertyName = "payable")]
        public bool Payable { get; set; }


        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        /// <summary>
        /// کد وضعیت به ازای حالت های مختلف
        /// </summary>
        /// <value>The status code.</value>
        [JsonProperty(PropertyName = "statusCode")]
        public string StatusCode { get; set; }

    }
}
