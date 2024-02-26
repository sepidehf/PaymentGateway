using System;
using System.Threading.Tasks;
using BankGateway.Domain.Models.DTO.BaamDTO;


namespace BankGateway.Domain.Services
{
   public class MellatBankService
    {
       public decimal GetBalance(string accountNumber)
       {
           throw new NotImplementedException();
       }

       public Task<PaymentOrderRegisterOutput> PaymentOrderRegister(PaymentOrderRegisterInput paymentOrderRegister)
       {
           throw new NotImplementedException();
       }

       public Task<PaymentOrderRegisterOutput> PaymentOrderInquery(string paymentOrderId)
       {
           throw new NotImplementedException();
       }

       public PaymentOrderRegisterOutput PaymentOrderComplete(PaymentOrderRegisterInput paymentOrderRegister)
       {
           throw new NotImplementedException();
       }

       public BaamBatchTransactionOutput FileTransaction(BaamBatchTransactionInput fileTransaction)
       {
           throw new NotImplementedException();
       }

       public BaamBatchTransactionOutput FileTransactionInquery(BaamBatchTransactionInput fileTransaction)
       {
           throw new NotImplementedException();
       }

       public RecordInquiryResponseModel RecordInquery(RecordInqueryInput recordInquery)
       {
           throw new NotImplementedException();
       }

       public TransactionsInquiryOutput TransactionsInquery(TransactionsInquiryInput transactionInqueryInput)
       {
           throw new NotImplementedException();
       }

       public TransactionsInquiryOutput TransactionInquery(Guid recordId)
       {
           throw new NotImplementedException();
       }
    }
}
