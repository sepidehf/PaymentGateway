using System;
using System.Threading.Tasks;
using BankGateway.Domain.Models.DTO.BaamDTO;

namespace BankGateway.Domain.Services.Interface
{
    public interface IBankService
    {
        

        /// <summary>
        /// ایجاد یک دستور پرداخت جدید
        /// SADAD method signature: POST---- /orders
        /// </summary>
        /// <param name="paymentOrderRegister"></param>
        /// <returns></returns>
        Task<PaymentOrderRegisterOutput> RegisterOrder(PaymentOrderRegisterInput paymentOrderRegister);








        /// <summary>
        /// استعلام یک دستور پرداخت مشخص
        /// SADAD Method signature : Get , /orders/{orderId}
        /// </summary>
        /// <param name="paymentOrderId">شناسه دستور پرداخت که همان شناسه سفارش است</param>
        /// <returns>Task&lt;PaymentOrderRegisterOutput&gt;.</returns>
        Task<PaymentOrderRegisterOutput> PaymentOrderInquiry(string paymentOrderId);
        ///// <summary>
        ///// صدور یا استعلام تاییدیه پرداخت یک تراکنش مشخص
        ///// </summary>
        ///// <param name="paymentOrderRegister"></param>
        ///// <returns></returns>
        //PaymentOrderRegisterOutput PaymentOrderCompleteRequest(PaymentOrderRegisterInput paymentOrderRegister);



        /// <summary>
        /// اعلام تکمیل شدن فایل‌های یک دستور پرداخت مشخص
        /// SADAD method signature: PATCH , /orders/{orderId}
        /// </summary>
        /// <param name="orderId">The order identifier.</param>
        /// <returns>Task&lt;PaymentOrderRegisterOutput&gt;.</returns>
        Task<PaymentOrderRegisterOutput> CompletePaymentOrder(string orderId);
        /// <summary>
        ///  ارسال یک بسته اطلاعاتی مشخص
        ///  SADAD method signature ---- Put---- /orders/{orderId}/details/{detailId}  
        /// </summary>
        /// <param name="transactionInput"></param>
        /// <returns>BaamBatchTransactionOutput.</returns>
        Task<BaamBatchTransactionOutput> SendTransactionInformation(BaamBatchTransactionInput transactionInput);

        /// <summary>
        /// استعلام یک بسته اطلاعاتی مشخص
        /// SADAD method signature: GET, /orders/{orderId}/details/{detailId} 
        /// </summary>
        /// <param name="orderId">The order identifier.</param>
        /// <param name="packageId">The package identifier.</param>
        /// <returns>Task&lt;BaamBatchTransactionOutput&gt;.</returns>
        Task<BaamBatchTransactionOutput> PackageTransactionInquiry(int orderId, int packageId);
        /// <summary>
        /// دریافت لیست تراکنش‌ها براساس شناسه یکتای کلاینت، شناسه یکتای شرکت، نوع تراکنش، شناسه یکتای تراکنش والد و وضعیت انجام آنها 
        /// SADAD method signature: GET,/orders/{orderId}/details/{detailId}/records/{recordId}
        /// </summary>
        /// <param name="recordInquiry">The record inquiry.</param>
        /// <returns>RecordInquiryResponseModel.</returns>
        Task<RecordInquiryResponseModel> RecordInquiry(RecordInqueryInput recordInquiry);


        //-----------------------------------------Transactions------------------------------------------//
        //-----------------------------------------------------------------------------------------------//

        /// <summary>
        /// دریافت لیست تراکنش‌ها براساس شناسه یکتای کلاینت، شناسه یکتای شرکت، نوع تراکنش، شناسه یکتای تراکنش والد و وضعیت انجام آنها
        /// SADAD method signature: GET, /transactions
        /// </summary>
        /// <param name="transactionInqueryInput">The transaction inquery input.</param>
        /// <returns>TransactionsInqueryOutput.</returns>
        Task<TransactionsInquiryOutput> TransactionsInquiry(TransactionsInquiryInput transactionInqueryInput);





        /// <summary>
        /// استعلام یک رکورد مشخص
        /// SADAD Method signature : GET , /transactions/{transactonId}
        /// /transactions
        /// </summary>
        /// <param name="recordId"></param>
        /// <returns>TransactionInquiryOutput.</returns>
        Task<TransactionInquiryResponseModel> TransactionInquiry(Guid recordId);


        /// <summary>
        /// استعلام تاییدیه پرداخت یک تراکنش مشخص
        /// SADAD method signature: GET, /transactions/{transactionId}/confirmation
        /// </summary>
        /// <param name="recordId">The record identifier.</param>
        /// <returns>Task&lt;TransactionConfirmationResponseModel&gt;.</returns>
        Task<TransactionConfirmationBindingModel> TransactionConfirmationInquiry(Guid recordId);

        /// <summary>
        /// صدور تاییدیه پرداخت برای یک تراکنش مشخص
        /// SADAD method signature: POST, /transactions/{transactionId}/confirmation
        /// </summary>
        /// <param name="recordId">The record identifier that same transactionId in SADAD request Parameter</param>
        /// <param name="requestBindingModel">The request binding model.</param>
        /// <returns>Task&lt;TransactionConfirmationBindingModel&gt;.</returns>
        Task<TransactionConfirmationBindingModel> TransactionConfirmationRequest(Guid recordId, TransactionConfirmationBindingModel requestBindingModel);

        Task<TransactionInquiryResponseModel> RecordTransactionInquiry(RecordInqueryInput recordInquery);

    }
}
