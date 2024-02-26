using System;
using System.Collections.Generic;

using BankGateway.Domain.Entities;
using BankGateway.Domain.Models.BindingModel;
using BankGateway.Domain.Models.DTO;
using BankGateway.Domain.Models.Enum;
using Core.Infrastructure.Common;

namespace BankGateway.Domain.Services.Interface
{
    public interface IProcessOrderService
    {
        /// <summary>
        /// آماده سازی یک دستور پرداخت برای فایل های ورودی
        /// </summary>
        /// <param name="orderFileData">اطلاعات دستور پرداخت که با پردازش فایل ورودی بدست می آید</param>
        /// <param name="orderId">شناسه ای که برابر با آیدی فایل قابل پردازش است</param>
        /// <param name="projectName">نام پروژه ای که کار فراخوانی سرویس های ما را ارائه می دهد.</param>
        /// <returns>GenericResult&lt;PrepareOrderResultModel&gt;.</returns>
        GenericResult<PrepareOrderResultModel> PrepareOrder(TransferFileData orderFileData, Guid orderId, ExternalProjectName projectName);
        /// <summary>
        /// جهت بروزرسانی وضعیت یک دستور پرداخت ازین تابع استفاده می شود
        /// </summary>
        /// <param name="targetOrder">دستور پرداخت هدف</param>
        /// <param name="targetPackage">بسته متناظر با دستور پرداخت هدف</param>
        /// <param name="targetStatus">وضعیتی که می تواند بروز رسانی شود <see cref="CasStatus"/></param>
        /// <returns>در صورت موفقیت آمیز بودن عدد 1 و در صورت شکست هر چیزی غیر از عدد 1 به عنوان خروجی بازگردانده می شود</returns>
        int UpdateOrderStatus(Order targetOrder, Package targetPackage, CasStatuse targetStatus);
        /// <summary>
        /// Updates the package status.
        /// </summary>
        /// <param name="targetPackage">The target package.</param>
        /// <param name="targetStatus">The target status.</param>
        /// <returns>System.Int32.</returns>
        int UpdatePackageStatus(Package targetPackage, CasStatuse targetStatus);



        int UpdateOrderStatus(Order targetOrder, CasStatuse targetStatus);
         Order GetOrderByProjectOrderId(string projectOrderId, ExternalProjectName projectName);


        CheckParameterBindingModel GetOrderSumAndCount(Order order);
        List<Record> GetNotSentToBankRecords(Guid orderId);
        Account GetAccountByIban(string iban);


    }
}