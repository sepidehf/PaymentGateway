using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Core.Infrastructure.Common;

using BankGateway.Domain.Helpers;
using BankGateway.Domain.Services.Interface;
using BankGateway.Services.Wcf.Helper;
using Core.CrossCutting.Infrustructure.Logger;
using Core.Infrastructure.Common;
using AutoMapper;
using  BankGateway.Domain;
namespace BankGetWay.Service.Api.Controllers
{
    [RoutePrefix("BankGetWay")]
    public class BankGetWay : ApiController
    {
        private readonly IApplicationService _applicationService;
        private readonly ILogger _bankGatewayLogger;
        private readonly ILogger _exceptionLogger;
        // GET api/<controller>

        [HttpPost]
        [Route("DoFileTransaction")]
        public FileTransactionOutput DoFileTransaction(FileTransactionInput fileTransactionInput)
        {
            try
            {
                _bankGatewayLogger.Info($"Receive Request From{fileTransactionInput.ProjectName} and ip:{IPHelper.IPPort} and username:{fileTransactionInput.UserName} and file id:{fileTransactionInput.ServerFileId}");
                Mapper.CreateMap<FileTransactionInput, BankGateway.Domain.Models.DTO.FileTransactionInput>();
                var fileTransaction = Mapper.Map<FileTransactionInput, BankGateway.Domain.Models.DTO.FileTransactionInput>(fileTransactionInput);
                var result = _applicationService.DoFileTransaction(fileTransaction);
                if (result.IsSuccess)
                    return new FileTransactionOutput()
                    {
                        IsSuccess = true,
                        Message = result.Message,
                        ErrorCode = result.ErrorCode,
                    };

                var response = new FileTransactionOutput()
                {
                    IsSuccess = false,
                    Message = result.Message,
                    ErrorCode = result.ErrorCode,
                };

                return response;
            }
            catch (Exception ex)
            {
                _exceptionLogger.Error("Gateway Exception Logger : " + ex.Message, ex);
                return new FileTransactionOutput()
                {
                    IsSuccess = false,
                    Message = EnumHelper<ErrorCode>.GetEnumDescription(
                        ErrorCode.SepasInternalServerError.ToString()),
                    ErrorCode = ErrorCode.SepasInternalServerError
                };
            }

        }

        [HttpPost]
        [Route("OrderComplete")]
        public OrderCompleteOutput OrderComplete(OrderCompleteInput orderComplete)
        {
            try
            {
                _bankGatewayLogger.Info($"Receive Request From {IPHelper.IPPort}");
                if (orderComplete == null)
                    return new OrderCompleteOutput()
                    {
                        IsSuccess = false,
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(
                            ErrorCode.EmptyInput.ToString()),
                        ErrorCode = ErrorCode.EmptyInput
                    };
                var result = _applicationService.OrderComplete(orderComplete.OrderId, orderComplete.ProjectName);
                if (result.IsSuccess)
                {
                    return new OrderCompleteOutput()
                    {
                        IsSuccess = result.IsSuccess,
                        Message = result.Message,
                        ErrorCode = result.ErrorCode,
                    };
                }
                return new OrderCompleteOutput()
                {
                    IsSuccess = result.IsSuccess,
                    Message = result.Message,
                    ErrorCode = result.ErrorCode,
                };
            }
            catch (Exception ex)
            {
                _exceptionLogger.Error("Global Exception Logger", ex.InnerException);
                return new OrderCompleteOutput()
                {

                    IsSuccess = false,
                    Message = EnumHelper<ErrorCode>.GetEnumDescription(
                        ErrorCode.SepasInternalServerError.ToString()),
                    ErrorCode = ErrorCode.SepasInternalServerError
                };
            }

        }

        [HttpPost]
        [Route("OrderInquiry")]
        public OrderInquiryOutput OrderInquiry(OrderInquiryInput orderInquiry)
        {

            try
            {
                _bankGatewayLogger.Info($"Receive Request From {IPHelper.IPPort}");
                if (orderInquiry == null)
                    return new OrderInquiryOutput()
                    {
                        IsSuccess = false,
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(
                            ErrorCode.EmptyInput.ToString()),
                        ErrorCode = ErrorCode.EmptyInput
                    };
                var result = _applicationService.OrderInquiry(orderInquiry.OrderId, orderInquiry.ProjectName);
                if (result.IsSuccess)
                {
                    var output = new OrderInquiryOutput()
                    {
                        IsSuccess = result.IsSuccess,
                        Message = result.Message,
                        ErrorCode = result.ErrorCode,
                        Status = result.Status,
                        StatusCode = result.StatusCode,
                        TotalAmount = result.TotalAmount,
                        TotalCount = result.TotalCount,
                        SubTransactions = result.SubTransactions

                    };

                    return output;
                }

                return new OrderInquiryOutput()
                {
                    IsSuccess = result.IsSuccess,
                    Message = result.Message,
                    ErrorCode = result.ErrorCode,
                };
            }
            catch (Exception ex)
            {
                _exceptionLogger.Error("Gateway Exception Logger : " + ex.Message, ex);
                throw new Exception("INTERNAL SERVER ERROR!");
            }
        }

        //[WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json,
        //    ResponseFormat = WebMessageFormat.Json, UriTemplate = "ResendOrderRecords/{orderId}/{projectName}")]

        [HttpPost]
        [Route("ResendOrderRecords")]
        public Result ResendOrderRecords(string orderId, ExternalProjectName projectName)
        {
            _bankGatewayLogger.Info($"Receive Request From {IPHelper.IPPort}");
            try
            {
                var resendOrderToBankResult = _applicationService.ResendOrderRecords(orderId, projectName);
                if (resendOrderToBankResult.IsSuccess)
                    return new Result()
                    {
                        IsSuccess = true,
                        Message = resendOrderToBankResult.Message,
                        ErrorCode = resendOrderToBankResult.ErrorCode
                    };
                return new Result()
                {
                    IsSuccess = false,
                    Message = resendOrderToBankResult.Message,
                    ErrorCode = resendOrderToBankResult.ErrorCode
                };
            }
            catch (Exception ex)
            {
                _exceptionLogger.Error("Global Exception Logger", ex.InnerException);
                throw new Exception("INTERNAL SERVER ERROR!");
            }
        }

        [HttpPost]
        [Route("OrderConfirmation")]
        public Result OrderConfirmation(string orderId, ExternalProjectName projectName)
        {
            _bankGatewayLogger.Info($"Receive Request From {IPHelper.IPPort}");
            try
            {
                var orderConfirmationResult = _applicationService.OrderConfirmation(orderId, projectName);
                if (orderConfirmationResult.IsSuccess)
                    return new Result()
                    {
                        IsSuccess = true,
                        Message = orderConfirmationResult.Message,
                        ErrorCode = orderConfirmationResult.ErrorCode
                    };
                return new Result()
                {
                    IsSuccess = false,
                    Message = orderConfirmationResult.Message,
                    ErrorCode = orderConfirmationResult.ErrorCode
                };
            }
            catch (Exception ex)
            {
                _exceptionLogger.Error("Global Exception Logger", ex.InnerException);
                return new Result()
                {
                    IsSuccess = false,
                    Message = EnumHelper<ErrorCode>.GetEnumDescription(
                         ErrorCode.SepasInternalServerError.ToString()),
                    ErrorCode = ErrorCode.SepasInternalServerError
                };
            }
        }

        [HttpPost]
        [Route("PackageCancel")]
        public Result PackageCancel(Guid projectPackageId, ExternalProjectName projectName)
        {
            _bankGatewayLogger.Info($"Receive Request From {IPHelper.IPPort}");
            try
            {
                var packageCancleResult = _applicationService.PackageCancle(projectPackageId, projectName);
                if (packageCancleResult.IsSuccess)
                    return new Result()
                    {
                        IsSuccess = true,
                        Message = packageCancleResult.Message,
                        ErrorCode = packageCancleResult.ErrorCode
                    };
                return new Result()
                {
                    IsSuccess = false,
                    Message = packageCancleResult.Message,
                    ErrorCode = packageCancleResult.ErrorCode
                };
            }
            catch (Exception ex)
            {
                _exceptionLogger.Error("Global Exception Logger", ex.InnerException);
                return new Result()
                {
                    IsSuccess = false,
                    Message = EnumHelper<ErrorCode>.GetEnumDescription(
                         ErrorCode.SepasInternalServerError.ToString()),
                    ErrorCode = ErrorCode.SepasInternalServerError
                };
            }
        }

        [HttpPost]
        [Route("OrderCancel")]
        public Result OrderCancel(string projectOrderId, ExternalProjectName projectName)
        {
            _bankGatewayLogger.Info($"Receive Request From {IPHelper.IPPort}");
            try
            {
                var orderCancleResult = _applicationService.OrderCancle(projectOrderId, projectName);
                if (orderCancleResult.IsSuccess)
                    return new Result()
                    {
                        IsSuccess = true,
                        Message = orderCancleResult.Message,
                        ErrorCode = orderCancleResult.ErrorCode
                    };
                return new Result()
                {
                    IsSuccess = false,
                    Message = orderCancleResult.Message,
                    ErrorCode = orderCancleResult.ErrorCode
                };
            }
            catch (Exception ex)
            {
                _exceptionLogger.Error("Global Exception Logger", ex.InnerException);
                return new Result()
                {
                    IsSuccess = false,
                    Message = EnumHelper<ErrorCode>.GetEnumDescription(
                         ErrorCode.SepasInternalServerError.ToString()),
                    ErrorCode = ErrorCode.SepasInternalServerError
                };
            }
        }
    }
}