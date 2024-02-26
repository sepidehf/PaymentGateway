using System;
using BankGateway.Domain.Models.DTO.Results;
using BankGateway.Domain.Models.Enum;
using Core.Infrastructure.Common;
using FileTransactionInput = BankGateway.Domain.Models.DTO.FileTransactionInput;
using Result = BankGateway.Domain.Models.DTO.Results.Result;

namespace BankGateway.Domain.Services.Interface
{
    public interface IApplicationService
    {
        /// <summary>
        /// شروع عملیات گرفتن فایل و ارسال سفارش به بانک
        /// </summary>
        /// <param name="fileTransaction">The file transaction.</param>
        /// <returns>Result.</returns>
        Result DoFileTransaction(FileTransactionInput fileTransaction);
        RecordInquiryResult RecordInquiry(string projectRecordId, Core.Infrastructure.Common.ExternalProjectName projectName);
        Result OrderComplete(string projectOrderId, Core.Infrastructure.Common.ExternalProjectName projectName);
        Result ResendOrderRecords(string projectOrderId, Core.Infrastructure.Common.ExternalProjectName projectName);
        OrderInquiryResult OrderInquiry(string projectOrderId, Core.Infrastructure.Common.ExternalProjectName projectName);
        Result OrderConfirmation(string projectOrderId, Core.Infrastructure.Common.ExternalProjectName projectName);
        Result PackageCancle(Guid projectPackageId, ExternalProjectName projectName);
        Result OrderCancle(string projectOrderId, ExternalProjectName projectName);
    }
}
