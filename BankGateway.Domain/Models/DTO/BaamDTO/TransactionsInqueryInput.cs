using BankGateway.Domain.Models.Enum;
using Core.Infrastructure.Common;

namespace BankGateway.Domain.Models.DTO.BaamDTO
{
   public class TransactionsInquiryInput
    {
        public string ClientId { get; set; }
        /// <summary>
        /// شناسه یکتای تراکنش والد = order ID
        /// </summary>
        /// <value>The parent transaction identifier.</value>
        public string ParentTransactionId { get; set; }
        public string CorporateId { get; set; }
        public TransactionType TransactionType { get; set; }
    
        /// <summary>
        /// فیلتر کردن بر اساس status.
        /// </summary>
        /// <value>The status.</value>
        public BaamStatus Status { get; set; }
    }

}
