using System;
using System.Collections.Generic;
using System.ServiceModel.Web;
using AutoMapper;
using BankGateway.Domain.Helpers;
using BankGateway.Domain.Services.Interface;
using BankGateway.Services.Wcf.Helper;
using Core.CrossCutting.Infrustructure.Logger;
using Core.Infrastructure.Common;

using SubTransaction = BankGateway.Domain.Models.DTO.BaamDTO.SubTransaction;


namespace BankGateway.Services.Wcf
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    //[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class BankGatewayService : IBankGatewayService
    {
        private readonly IApplicationService _applicationService;
        private readonly ILogger _bankGatewayLogger;
        private readonly ILogger _exceptionLogger;
      
        public BankGatewayService(IApplicationService applicationService)
        {
            _applicationService = applicationService;
            _bankGatewayLogger = new Logger("BankGateWayLogger");
            _exceptionLogger = new Logger("ExceptionLogger");

        }

        public BankGatewayService()
        {

        }

        public FileTransactionOutput DoFileTransaction(FileTransactionInput fileTransactionInput)
        {
            try
            {
                _bankGatewayLogger.Info($"Receive Request From{fileTransactionInput.ProjectName} and ip:{IPHelper.IPPort} and username:{fileTransactionInput.UserName} and file id:{fileTransactionInput.ServerFileId}");
                Mapper.CreateMap<FileTransactionInput, Domain.Models.DTO.FileTransactionInput>();
                var fileTransaction = Mapper.Map<FileTransactionInput, Domain.Models.DTO.FileTransactionInput>(fileTransactionInput);
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

        //public FileTransactionOutput DoFileTransaction(FileTransactionInput fileTransactionInput)
        //{
           
            //try
            //{
            //    _bankGatewayLogger.Info($"Receive Request From{fileTransactionInput.ProjectName} and ip:{IPHelper.IPPort} and username:{fileTransactionInput.UserName} and file id:{fileTransactionInput.ServerFileId}");
            //    Mapper.CreateMap<FileTransactionInput, Domain.Models.DTO.FileTransactionInput>();
            //    var fileTransaction = Mapper.Map<FileTransactionInput, Domain.Models.DTO.FileTransactionInput>(fileTransactionInput);
            //    var result = _applicationService.DoFileTransaction(fileTransaction);
            //    if (result.IsSuccess)
            //        return new FileTransactionOutput()
            //        {
            //            IsSuccess = true,
            //            Message = result.Message,
            //            ErrorCode = result.ErrorCode,
            //        };

            //    var response = new FileTransactionOutput()
            //    {
            //        IsSuccess = false,
            //        Message = result.Message,
            //        ErrorCode = result.ErrorCode,
            //    };

            //    return response;
            //}
            //catch (Exception ex)
            //{
            //    _exceptionLogger.Error("Gateway Exception Logger : " + ex.Message, ex);
            //    throw new Exception("INTERNAL SERVER ERROR!");
            //}


      //  }


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


        //public RecordInquiryOutput RecordInquiry(RecordInquiryInput recordInquiry)
        //{
        //    try
        //    {
        //        _bankGatewayLogger.Info($"Receive Request From {IPHelper.IPPort}");
        //        if (recordInquiry == null)
        //            return new RecordInquiryOutput()
        //            {
        //                IsSuccess = false,
        //                Message = EnumHelper<ErrorCode>.GetEnumDescription(
        //                    ErrorCode.EmptyInput.ToString()),
        //                ErrorCode = ErrorCode.EmptyInput.ToString()
        //            };
        //        var result = _applicationService.RecordInquiry(recordInquiry.RecordId, recordInquiry.ProjectName);
        //        if (result.IsSuccess)

        //            return new RecordInquiryOutput()
        //            {
        //                IsSuccess = result.IsSuccess,
        //                Message = result.Message,
        //                ErrorCode = result.ErrorCode,
        //                RecordId = result.ProjectRecordId,
        //                Status = result.Status,
        //                OrderId=result.ProjectOrderId,
        //                Amount=result.Amount,
        //                DestinationIban=result.DestinationIban,
        //                Description=result.DEscription
        //            };
        //        return new RecordInquiryOutput()
        //        {
        //            IsSuccess = result.IsSuccess,
        //            Message = result.Message,
        //            ErrorCode = result.ErrorCode,
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        _exceptionLogger.Error("Gateway Exception Logger : " + ex.Message, ex);
        //        throw new Exception("INTERNAL SERVER ERROR!");
        //    }

        //}

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
                    var output= new OrderInquiryOutput()
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

        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json, UriTemplate = "ResendOrderRecords/{orderId}/{projectName}")]
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
                    Message =resendOrderToBankResult.Message,
                    ErrorCode = resendOrderToBankResult.ErrorCode
                };
            }
            catch (Exception ex)
            {
                _exceptionLogger.Error("Global Exception Logger", ex.InnerException);
                throw new Exception("INTERNAL SERVER ERROR!");
            }
        }

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
