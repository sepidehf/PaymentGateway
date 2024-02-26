using System;
using BankGateway.Domain.Models.DTO.Results;

namespace BankGateway.Domain.Models.DTO.BaamDTO
{
    public class TransactionConfirmationBindingModel : Result
    {
        /// <summary>
        /// شناسه یکتای تراکنش
        /// </summary>
        /// <value>The transaction identifier.</value>
        public string TransactionId { get; set; }
        /// <summary>
        /// تاریخ و زمان تاییدیه پرداخت تراکنش
        /// </summary>
        /// <value>The confirmation date time.</value>
        public DateTime ConfirmationDateTime { get; set; }
        /// <summary>
        /// وضعیت دارا بودن تاییدیه پرداخت تراکنش
        /// </summary>
        /// <value><c>true</c> if confirmed; otherwise, <c>false</c>.</value>
        public bool Confirmed { get; set; }


    }
}
