using System;
using Core.Infrastructure.Common;
using Newtonsoft.Json;

namespace BankGateway.Domain.Models.DTO.BaamDTO
{
    public class SubTransaction
    {
        /// <summary>
        /// شناسه یکتای تراکنش
        /// </summary>
        /// <value>شناسه یکتای تراکنش</value>
        ///TODO : Convert TransactionId TO ProjectRecordId and Data type is String
        /// 
          [JsonProperty(PropertyName = "transactionId")]
        public Guid TransactionId { get; set; }
        /// <summary>
        /// Gets or sets the type of the transaction.
        /// </summary>
        /// <value>نوع تراکنش</value>
        public TransactionType TransactionType { get; set; }
        /// <summary>
        /// عنوان تراکنش
        /// </summary>
        /// <value>عنوان تراکنش</value>
       
        public string Title { get; set; }
        /// <summary>
        /// شرح دستور پرداخت
        /// </summary>
        /// <value>شرح دستور پرداخت</value>
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
        /// <summary>
        /// مبلغ کل تراکنش
        /// </summary>
        /// <value>مبلغ کل تراکنش</value>
        [JsonProperty(PropertyName = "totalAmount")]
        public string TotalAmount { get; set; }
        /// <summary>
        /// تعداد کل تراکنش‌ها
        /// </summary>
        /// <value>تعداد کل تراکنش‌ها</value>
        public int TotalCount { get; set; }
        /// <summary>
        /// مبدا تراکنش
        /// </summary>
        /// <value>The source.</value>
        [JsonProperty(PropertyName = "source")]
        public string Source { get; set; }
        /// <summary>
        /// صاحب مبدا تراکنش
        /// </summary>
        /// <value>The source owner.</value>
        public string SourceOwner { get; set; }
        /// <summary>
        /// مقصد تراکنش
        /// </summary>
        /// <value>The destination.</value>
        [JsonProperty(PropertyName = "destination")]
        public string Destination { get; set; }
        /// <summary>
        /// صاحب مقصد تراکنش
        /// </summary>
        /// <value>The destination owner.</value>
        [JsonProperty(PropertyName = "destinationOwner")]
        public string DestinationOwner { get; set; }
        /// <summary>
        /// تاریخ و زمان ایجاد تراکنش
        /// </summary>
        /// <value>The creation date time.</value>
        public DateTime CreationDateTime { get; set; }
        /// <summary>
        /// تاریخ سررسید تراکنش
        /// </summary>
        /// <value>The due date time.</value>
        public DateTime DueDate { get; set; }
        /// <summary>
        /// تاریخ و زمان آخرین به‌روز رسانی تراکنش
        /// </summary>
        /// <value>The update date time.</value>
        public DateTime UpdatedDateTime { get; set; }
        /// <summary>
        /// شناسه پرداخت
        /// </summary>
        /// <value>The payment reference.</value>
        public string PaymentReference { get; set; }
        /// <summary>
        /// شماره پیگیری
        /// </summary>
        /// <value>The trace number.</value>
        [JsonProperty(PropertyName = "traceNumber")]
        public string TraceNumber { get; set; }
        /// <summary>
        /// وضعیت انجام تراکنش
        /// </summary>
        /// <value>one of RECEIVED, SUBMITED, REGISTERED, PROCESSING, SUCCEEDED, FAILED,
        /// CONTRADICTION, PARTIALY-SUCCEEDED, PARTIALY-REGISTERED, CANCELED, SUSPENDED</value>
        [JsonProperty(PropertyName = "status")]

       public string Status { get; set; }
        /// <summary>
        /// پیغام متناسب با وضعیت تراکنش
        /// </summary>
        /// <value>The response message.</value>
        [JsonProperty(PropertyName = "responseMessage")]

        public string ResponseMessage { get; set; }
        /// <summary>
        /// آمار تراکنش‌ها
        /// </summary>
        /// <value>The statistic.</value>
        public Statistic Statistic { get; set; }
    }
}