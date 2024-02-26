using System;
using Newtonsoft.Json;

namespace BankGateway.Domain.Models.DTO
{
    /// <summary>
    /// اطلاعات پردازش شده رکوردهای فایل ها قبل از ثبت در دیتابیس 
    /// </summary>
    public class PaymentInfo
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>   
        [JsonProperty(PropertyName = "ID")]
        public string ProjectRecordId { get; set; }
        public string DestinationIBAN { get; set; }
        public double Amount { get; set; }
        public string DestinationTitle { get; set; }
        public string Description { get; set; }

        public string SourceIBAN { get; set; }
        /// <summary>
        /// شناسه واریز
        /// </summary>
        /// <value>The payment identifier.</value>
        public string PaymentId { get; set; }
    }
}