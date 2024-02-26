using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Configuration;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BankGateway.Domain.Entities;
using BankGateway.Domain.Factory;
using BankGateway.Domain.Helpers;
using BankGateway.Domain.Models.DTO;
using BankGateway.Domain.Models.DTO.BaamDTO;
using BankGateway.Domain.Models.DTO.Results;
using BankGateway.Domain.Models.Enum;
using BankGateway.Domain.Services.Interface;
using Core.CrossCutting.Infrustructure.Logger;
using Core.Infrastructure.Common;
using FileTransactionInput = BankGateway.Domain.Models.DTO.FileTransactionInput;
using Result = BankGateway.Domain.Models.DTO.Results.Result;
using SubTransaction = BankGateway.Domain.Models.DTO.BaamDTO.SubTransaction;


namespace BankGateway.Domain.Services
{
    public class ApplicationService : IApplicationService
    {

        private readonly string _fileServiceBaseAddress = ConfigurationManager.AppSettings["FileServerBaseAddress"];
        private readonly IBankServiceFactory _bankServiceFactory;
        private readonly IRecordService _recordService;
        private readonly IPackageService _packageService;
        private readonly IProcessOrderService _processOrderService;
        private readonly ILogger _applicationServiceLogger;


        public ApplicationService(IBankServiceFactory bankServiceFactory, IRecordService recordService,
            IProcessOrderService processOrderService,IPackageService packageService)
        {
            _bankServiceFactory = bankServiceFactory;
            _recordService = recordService;
            _processOrderService = processOrderService;
            _packageService = packageService;
            _applicationServiceLogger = new Logger(typeof(ApplicationService).FullName);
        }

        /// <summary>
        /// شروع عملیات گرفتن فایل و ارسال سفارش به بانک
        /// </summary>
        /// <param name="fileTransaction">The file transaction.</param>
        /// <returns>Result.</returns>
        public Result DoFileTransaction(FileTransactionInput fileTransaction)
        {
            if (fileTransaction == null)
                return new Result()
                {
                    Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.EmptyInput.ToString()),
                    ErrorCode = ErrorCode.EmptyInput,
                    IsSuccess = false
                };
            if(string.IsNullOrWhiteSpace(fileTransaction.Username))
                return new Result()
                {
                    Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.UsernameEmpty.ToString()),
                    ErrorCode = ErrorCode.UsernameEmpty,
                    IsSuccess = false
                };
            try
            {
                var transferFileDataResult = GetFileFromServer<TransferFileData>(
                        $"{_fileServiceBaseAddress}/GetFullFile?fileId={fileTransaction.ServerFileId}");

                _applicationServiceLogger.Info($"GetFileFromServer, id:  {fileTransaction.ServerFileId}");
               // var fileText = System.IO.File.ReadAllText(@"H:\File\MOCK_DATA.json");

                //var transferFileDataResult = JsonConvert.DeserializeObject<TransferFileData>(fileText);

                if (transferFileDataResult.PaymentList.Sum(x => x.Amount) !=
                    Convert.ToDouble(transferFileDataResult.TotalAmount))
                {
                    return new Result()
                    {
                        IsSuccess = false,
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.TotalAmountError.ToString()),
                        ErrorCode = ErrorCode.TotalAmountError,
                    };
                }
                if (_processOrderService.GetAccountByIban(transferFileDataResult.SourceIBAN)==null)
                {
                    return new Result()
                    {
                        IsSuccess = false,
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.ShebaError.ToString()),
                        ErrorCode = ErrorCode.ShebaError,
                    };
                }

                //var dateDue = DateTime.ParseExact(transferFileDataResult.DueDate, "0:yyyy-MM-ddTHH:mm:ss.FFFZ",
                //    System.Globalization.CultureInfo.InvariantCulture);
              

                DateTime dateTime = DateTime.ParseExact(transferFileDataResult.DueDate, "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
                    CultureInfo.CurrentCulture);

                if (dateTime.Date < DateTime.Now.Date)
                {
                    return new Result()
                    {
                        IsSuccess = false,
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.DueDateError.ToString()),
                        ErrorCode = ErrorCode.DueDateError,
                    };
                }

                var prepareOrderResult = _processOrderService.PrepareOrder(transferFileDataResult,
                    fileTransaction.ServerFileId,fileTransaction.ProjectName);

                if (prepareOrderResult.IsSuccess)
                {
                    //اولین بار است که سفارش ساخته می شود یا به بانک ارسال می شود
                    if (prepareOrderResult.ReturnObject.ResultOrder.Status == CasStatuse.InsertToSepas)
                    {
                        //TODO: GET BANK NAME
                        _applicationServiceLogger.Info($"Prepare for registering order with Id : {prepareOrderResult.ReturnObject.ResultOrder.Id}");

                        var registerOrderResult = RegisterOrder(transferFileDataResult, prepareOrderResult.ReturnObject.ResultOrder.Id, new BaamService());


                        if (registerOrderResult.IsSuccess || registerOrderResult.ErrorCode== ErrorCode.E0000009)
                        {
                            _processOrderService.UpdateOrderStatus(prepareOrderResult.ReturnObject.ResultOrder,
                                CasStatuse.SendToBank);

                            _applicationServiceLogger.Info($"Order with Id : {prepareOrderResult.ReturnObject.ResultOrder.Id} Register Successfully");

                            if (prepareOrderResult.ReturnObject.ResultPackage.Status == CasStatuse.InsertToSepas)
                            {
                                var sendOrderResult = SendPackageToBank(prepareOrderResult.ReturnObject.ResultPackage, new BaamService());
                                if (sendOrderResult.IsSuccess)
                                {
                                    _processOrderService.UpdatePackageStatus(
                                     prepareOrderResult.ReturnObject.ResultPackage, CasStatuse.SendToBank);
                                    _applicationServiceLogger.Info($"Order with Id : ''{prepareOrderResult.ReturnObject.ResultOrder.Id}'' Sent to bank successfully");

                                    return new Result()
                                    {
                                        Message = EnumHelper<ErrorCode>.GetEnumDescription(
                                            ErrorCode.IsSuccess.ToString()),
                                        ErrorCode = ErrorCode.IsSuccess,
                                        IsSuccess = true
                                    };
                                }
                                else
                                {
                                    return new Result()
                                    {
                                        Message = sendOrderResult.Message,
                                        ErrorCode = sendOrderResult.ErrorCode,
                                        IsSuccess = false
                                    };
                                }
                            }
                            else
                            {
                                return new Result()
                                {
                                    IsSuccess = false,
                                    Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.CanNotSendToBank.ToString()),
                                    ErrorCode = ErrorCode.CanNotSendToBank,
                                };

                            }



                        }
                        else
                        //خطایی از طرف بانک اعلام شده است
                        {
                            var response = new Result()
                            {
                                Message = registerOrderResult.Message,
                                ErrorCode = registerOrderResult.ErrorCode,
                                IsSuccess = false
                            };
                            _applicationServiceLogger.Warn(JsonConvert.SerializeObject(response));
                            return response;
                        }
                    }
                    else
                    {
                        var sendOrderResult = SendPackageToBank(prepareOrderResult.ReturnObject.ResultPackage, new BaamService());
                        if (sendOrderResult.IsSuccess)
                        {
                            _processOrderService.UpdatePackageStatus(prepareOrderResult.ReturnObject.ResultPackage,
                                CasStatuse.SendToBank);
                            return new Result()
                            {
                                Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.IsSuccess.ToString()),
                                ErrorCode = ErrorCode.IsSuccess,
                                IsSuccess = true
                            };
                        }
                        else
                        {
                            var response = new Result()
                            {
                                Message = sendOrderResult.Message,
                                ErrorCode = sendOrderResult.ErrorCode,
                                IsSuccess = false
                            };
                            _applicationServiceLogger.Warn(JsonConvert.SerializeObject(response));
                            return response;
                        }


                    }
                }
                else
                {
                    var response = new Result()
                    {
                        Message = prepareOrderResult.Message,
                        ErrorCode = prepareOrderResult.ErrorCode,
                        IsSuccess = false
                    };

                    return response;
                }

            }
            catch (DbUpdateException exception)
            {
               return new Result()
                {
                    Message = exception.Message,
                    ErrorCode = ErrorCode.SepasDatabaseError,
                    IsSuccess = false
                };
            }
            catch (HttpRequestException exception)
            {
                return new Result()
                {
                    Message = exception.Message,
                    ErrorCode = ErrorCode.SepasInternalServerError,
                    IsSuccess = false
                };
            }
            catch (Exception exception)
            {
                return new Result()
                {
                    Message = exception.Message,
                    ErrorCode = ErrorCode.SepasInternalServerError,
                    IsSuccess = false
                };
            }

        }

        //todo review
        public RecordInquiryResult RecordInquiry(string projectRecordId, ExternalProjectName projectName)
        {
            try
            {
                var record = _recordService.GetRecordByProjectRecordId(projectRecordId, projectName);
                if (record == null)
                    return new RecordInquiryResult()
                    {
                        IsSuccess = false,
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.NoFoundRecord.ToString()),
                        ErrorCode = ErrorCode.NoFoundRecord,
                    };

             //   var bankName = record.Package.Order.Account.BankName;
                var bankService = _bankServiceFactory.Create("Baam");

                var recordInquiryInput = new RecordInqueryInput()
                {
                    RecordId = record.Id,
                    OrderId = record.Package.OrderId,
                    PackageId = record.PackageId
                };
                //حالتی که رکورد سمت بانک ارسال نشده و یا اینکه پرداخت آن موفقیت آمیز بوده است.
                if (record.PaymentStatus == PaymentStatus.Succeeded )
                    return new RecordInquiryResult()
                    {
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.IsSuccess.ToString()),
                        ErrorCode = ErrorCode.IsSuccess,
                        IsSuccess = true,
                        ProjectRecordId = projectRecordId,
                        Status = record.PaymentStatus.ToString()
                    };
                if (record.Package.Status == CasStatuse.InsertToSepas)
                    return new RecordInquiryResult()
                    {
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.IsSuccess.ToString()),
                        ErrorCode = ErrorCode.IsSuccess,
                        IsSuccess = true,
                        ProjectRecordId = projectRecordId,
                        Status = record.Package.Status.ToString()
                    };
                //تراکنش انجام شده و سفارش داده شده بسته شده است
                if (record.Package.Order.Payable)
                {

                    Func<Task<TransactionInquiryResponseModel>> transactionOrderInquiryFunc =
                 () => bankService.TransactionInquiry(record.Package.OrderId);
                    var transactionOrderInquiryResult = RetryHelper.ExecuteWithRetry(transactionOrderInquiryFunc, 3).Result;
                    if (transactionOrderInquiryResult.IsSuccess)
                    {
                        if (transactionOrderInquiryResult.Status != "SUCCEEDED")//todo in processing 
                        {

                            Func<Task<TransactionInquiryResponseModel>> transactionInquiryFunc =
                                () => bankService.TransactionInquiry(record.Id);
                            var transactionInquiryResult = RetryHelper.ExecuteWithRetry(transactionInquiryFunc, 3).Result;
                            if (transactionInquiryResult.IsSuccess)
                            {
                                foreach (PaymentStatus suit in Enum.GetValues(typeof(PaymentStatus)))
                                {
                                    if (suit.ToString() !=
                                        transactionInquiryResult.Status) continue;
                                    record.PaymentStatus = suit;
                                    break;
                                }

                                _recordService.Update(record);

                                return new RecordInquiryResult()
                                {
                                    Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.IsSuccess.ToString()),
                                    ErrorCode = ErrorCode.IsSuccess,
                                    IsSuccess = true,
                                    ProjectRecordId = projectRecordId,
                                    Status = transactionInquiryResult.Status
                                };



                            }

                            return new RecordInquiryResult()
                            {
                                IsSuccess = false,
                                Message = transactionInquiryResult.Message,
                                ErrorCode = transactionInquiryResult.ErrorCode,
                            };
                        }
                        else
                        {

                            return new RecordInquiryResult()
                            {
                                Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.IsSuccess.ToString()),
                                ErrorCode = ErrorCode.IsSuccess,
                                IsSuccess = true,
                                ProjectRecordId = projectRecordId,
                                Status = transactionOrderInquiryResult.Status
                            };

                        }
                    }

                    else
                    {
                        return new RecordInquiryResult()
                        {
                            IsSuccess = false,
                            Message = transactionOrderInquiryResult.Message,
                            ErrorCode = transactionOrderInquiryResult.ErrorCode,
                        };
                    }

                }
                //تراکنش انجام نشده و سفارش داده شده باز است
                Func<Task<RecordInquiryResponseModel>> recordInquiryFunc = () => bankService.RecordInquiry(recordInquiryInput);
                var recordInquiryResult = RetryHelper.ExecuteWithRetry(recordInquiryFunc, 3).Result;
                if (!recordInquiryResult.IsSuccess)
                    return new RecordInquiryResult()
                    {
                        IsSuccess = false,
                        Message = recordInquiryResult.Message,
                        ErrorCode = recordInquiryResult.ErrorCode,
                    };
                {
                    //comment becuase befor complete order ,recorde dose not have inquiry status, if result is successful it means that bank recieve record
                    //foreach (PaymentStatus suit in Enum.GetValues(typeof(PaymentStatus)))
                    //{
                    //    if (EnumHelper<PaymentStatus>.GetEnumDescription(suit.ToString()) !=
                    //        recordInquiryResult.Status) continue;
                    //    record.PaymentStatus = suit;
                    //    break;
                    //}
                   // _recordService.Update(record);
                    record.PaymentStatus = PaymentStatus.SentToBank;
                    return new RecordInquiryResult()
                    {
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.IsSuccess.ToString()),
                        ErrorCode = ErrorCode.IsSuccess,
                        IsSuccess = true,
                         ProjectRecordId = projectRecordId,
                         ProjectOrderId=record.Package.Order.ProjectOrderId,
                         Amount=recordInquiryResult.Amount,
                         DEscription=recordInquiryResult.Description,
                         DestinationIban=recordInquiryResult.DestinationIban,
                        Status = PaymentStatus.SentToBank.ToString()
                    };
                }
            }
            catch (Exception exception)
            {
                throw;
            }

        }

        public OrderInquiryResult OrderInquiry(string projectOrderId, ExternalProjectName projectName)
        {
            try
            {
                var order = _processOrderService.GetOrderByProjectOrderId(projectOrderId, projectName);
                if (order == null)
                    return new OrderInquiryResult()
                    {
                        IsSuccess = false,
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.NoFoundRecord.ToString()),
                        ErrorCode = ErrorCode.NoFoundRecord,
                    };
                //TODO: مشکل دارد، حساب مبدا باید برای order ثبت شود
                var bankService = _bankServiceFactory.Create("Baam");
                //سفارش بسته شده است
                if (order.Payable)
                {
                    var transactionInquiryInput = new TransactionsInquiryInput()
                    {
                        //TODO
                        ClientId = "",
                        CorporateId = "",
                        ParentTransactionId = order.Id.ToString(),
                        Status = BaamStatus.All,
                        TransactionType = TransactionType.INDIRECT_BATCH
                    };

                    //transactionInquiry if status is successfull then transactionsInquiry for recordDetail
                    Func<Task<TransactionInquiryResponseModel>> transactionInquiryFunc =
                       () => bankService.TransactionInquiry(order.Id);
                    var transactionInquiryResult =
                        RetryHelper.ExecuteWithRetry(transactionInquiryFunc, 3).Result;
                    if (transactionInquiryResult.IsSuccess)
                    {
                        if (transactionInquiryResult.Status == "SUCCEEDED")
                        {
                            order.Status = CasStatuse.SUCCEEDED;
                            _processOrderService.UpdateOrderStatus(order, CasStatuse.SUCCEEDED);

                            Func<Task<TransactionsInquiryOutput>> transactionsInquiryFunc =
                           () => bankService.TransactionsInquiry(transactionInquiryInput);
                            var transactionsInquiryResult =
                                RetryHelper.ExecuteWithRetry(transactionsInquiryFunc, 3).Result;

                            if (transactionsInquiryResult.IsSuccess)
                            {
                                var records = new List<Record>();
                                foreach (var subTransaction in transactionsInquiryResult.SubTransactions.Where(x => x.Status == "SUCCEEDED"))
                                {
                                    var record = _recordService.GetRecord(subTransaction.TransactionId);
                                    record.PaymentStatus = PaymentStatus.Succeeded;
                                    records.Add(record);
                                }

                                _recordService.Update(records);//just succeded records


                                var result = new OrderInquiryResult()
                                {
                                    Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.IsSuccess.ToString()),
                                    ErrorCode = ErrorCode.IsSuccess,
                                    IsSuccess = true,
                                    TotalAmount = transactionsInquiryResult.TotalAmount,
                                    TotalCount = transactionsInquiryResult.TotalCount,

                                    StatusCode = transactionsInquiryResult.StatusCode,
                                    SubTransactions = PrepareOrderSubTransaction(order, transactionsInquiryResult.SubTransactions)
                                };


                                foreach (CasStatuse suit in Enum.GetValues(typeof(CasStatuse)))
                                {
                                    if (suit.ToString()!=
                                        transactionsInquiryResult.Status) continue;
                                    result.Status = suit;
                                    break;
                                }
                                return result;

                            }

                            return new OrderInquiryResult()
                            {
                                IsSuccess = false,
                                Message = transactionsInquiryResult.Message,
                                ErrorCode = transactionsInquiryResult.ErrorCode,
                            };
                        }

                        else
                        {
                            //if not succeded

                            var inquiryResult = new OrderInquiryResult()
                            {
                                Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.IsSuccess.ToString()),
                                ErrorCode = ErrorCode.IsSuccess,
                                IsSuccess = true,
                                TotalAmount = Convert.ToDouble(transactionInquiryResult.TotalAmount),
                                TotalCount = transactionInquiryResult.TotalCount,

                                StatusCode = transactionInquiryResult.ErrorCode.ToString(),

                            };


                            foreach (CasStatuse suit in Enum.GetValues(typeof(CasStatuse)))
                            {
                                if (suit.ToString() !=
                                    transactionInquiryResult.Status) continue;
                                inquiryResult.Status = suit;
                                break;
                            }
                            return inquiryResult;
                        }

                    }
                    else
                    {
                        return new OrderInquiryResult()
                        {
                            IsSuccess = false,
                            Message = transactionInquiryResult.Message,
                            ErrorCode = transactionInquiryResult.ErrorCode,
                        };
                    }




                }
          
                Func<Task<PaymentOrderRegisterOutput>> orderInquiryFunc = () => bankService.PaymentOrderInquiry(order.Id.ToString());
                var orderInquiryResult = RetryHelper.ExecuteWithRetry(orderInquiryFunc, 3).Result;


                if (orderInquiryResult.IsSuccess)
                {
                   var result= new OrderInquiryResult()
                    {
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.IsSuccess.ToString()),
                        ErrorCode = ErrorCode.IsSuccess,
                        IsSuccess = true,
                        TotalAmount = orderInquiryResult.TotalAmount,
                        TotalCount = orderInquiryResult.RecordsCount,
                      
                        StatusCode = orderInquiryResult.StatusCode.ToString()
                    };
                    foreach (CasStatuse suit in Enum.GetValues(typeof(CasStatuse)))
                    {
                        if (suit.ToString() !=
                            orderInquiryResult.Status) continue;
                        result.Status = suit;
                        break;
                    }
                    return result;

                }

                return new OrderInquiryResult()
                {
                    IsSuccess = false,
                    Message = orderInquiryResult.Message,
                    ErrorCode = orderInquiryResult.ErrorCode,
                };

            }
            catch (Exception exception)
            {
                return new OrderInquiryResult()
                {
                    Message = exception.Message,
                    ErrorCode = ErrorCode.SepasInternalServerError,
                    IsSuccess = false
                };
            }

        }

        private List<OrderSubTransaction> PrepareOrderSubTransaction(Order order,
            List<SubTransaction> subTransactions)
        {
            var result = new List<OrderSubTransaction>();
            var records = order.Packages.SelectMany(x => x.Records);

            foreach (var transaction in subTransactions)
            {
                var targetRecord = records.FirstOrDefault(x => x.Id == transaction.TransactionId);
                if (targetRecord != null)
                {
                    var item = new OrderSubTransaction()
                    {
                        TransactionId =targetRecord.ProjectRecordId,
                        CreationDateTime = transaction.CreationDateTime,
                        Description = transaction.Description,
                        Destination = transaction.Destination,
                        DestinationOwner = transaction.DestinationOwner,
                        DueDate = transaction.DueDate,
                        PaymentReference = transaction.PaymentReference,
                        Source = transaction.Source,
                        ResponseMessage = transaction.ResponseMessage,
                        SourceOwner = transaction.SourceOwner,
                        Statistic = transaction.Statistic,
                        //Status = transaction.Status,
                        Title = transaction.Title,
                        TotalAmount = transaction.TotalAmount,
                        TotalCount = transaction.TotalCount,
                        TraceNumber = transaction.TraceNumber,
                        TransactionType = transaction.TransactionType,
                        UpdatedDateTime = transaction.UpdatedDateTime
                    };

                    foreach (CasStatuse suit in Enum.GetValues(typeof(CasStatuse)))
                    {
                        if (suit.ToString() !=
                            transaction.Status) continue;
                        item.Status = suit;
                        break;
                    }

                    result.Add(item);
                   
                }
            }

            return result;
        }










        public Result ResendOrderRecords(string projectOrderId, Core.Infrastructure.Common.ExternalProjectName projectName)
        {
            try
            {
               
                var bankService = _bankServiceFactory.Create("Baam");
                var order = _processOrderService.GetOrderByProjectOrderId(projectOrderId, projectName);
                if (order == null)
                {
                    return new Result()
                    {
                        IsSuccess = false,
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.NoFoundRecord.ToString()),
                        ErrorCode = ErrorCode.NoFoundRecord,
                    };
                }
                else
                {
                    //سفارش بسته شده است
                    if (order.Payable)
                    {
                        return new Result()
                        {
                            IsSuccess = false,
                            Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.FinalizedOrderError.ToString()),
                            ErrorCode = ErrorCode.FinalizedOrderError,
                        };
                    }
                    if (order.Status==CasStatuse.ProjectCancle)
                    {
                        return new Result()
                        {
                            IsSuccess = false,
                            Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.OrderCancel.ToString()),
                            ErrorCode = ErrorCode.OrderCancel,
                        };
                    }

                    var notSentToBankRecords = _processOrderService.GetNotSentToBankRecords(order.Id);
                    var orderPackages = notSentToBankRecords.GroupBy(x => x.Package, (recordpackage, records) => new
                    {
                        package = recordpackage,
                        records = records.ToList()
                    });

                    foreach (var orderPackage in orderPackages)
                    {
                        if (orderPackage.package.Order.Status == CasStatuse.SendToBank)
                        {
                            var reSendPackageResult = SendPackageToBank(orderPackage.package, bankService);
                            if (reSendPackageResult.IsSuccess)
                            {
                                _processOrderService.UpdatePackageStatus(orderPackage.package, CasStatuse.SendToBank);
                            }
                            else
                            {
                                return new Result()
                                {
                                    Message = reSendPackageResult.Message,
                                    ErrorCode = reSendPackageResult.ErrorCode,
                                    IsSuccess = false
                                };
                            }
                        }
                        else
                        {
                            var orderRegisterResult = RegisterOrder(new TransferFileData()
                            {
                                Description = order.Description,
                                DueDate = string.Format("{0:yyyy-MM-ddTHH:mm:ss.FFFZ}", order.DueDate),
                                ProjectOrderId = order.ProjectOrderId,
                                SourceIBAN = order.Account.IbanNo,
                                SourceTitle = order.Title,
                                

                            }, order.Id, bankService);

                            if (orderRegisterResult.IsSuccess || orderRegisterResult.ErrorCode==ErrorCode.DuplicateRegisterOrder)
                            {
                                _processOrderService.UpdateOrderStatus(order, CasStatuse.SendToBank);
                                var reSendPackageResult = SendPackageToBank(orderPackage.package, bankService);
                                if (reSendPackageResult.IsSuccess)
                                {
                                    _processOrderService.UpdatePackageStatus(orderPackage.package, CasStatuse.SendToBank);
                                }
                                else
                                {
                                    return new Result()
                                    {
                                        Message = reSendPackageResult.Message,
                                        ErrorCode = reSendPackageResult.ErrorCode,
                                        IsSuccess = false
                                    };
                                }
                            }
                            else
                            {
                                   return new Result()
                                {
                                    Message = orderRegisterResult.Message,
                                    ErrorCode = orderRegisterResult.ErrorCode,
                                    IsSuccess = false
                                };
                            }
                        }

                    }

                    return new Result()
                    {
                        IsSuccess = true,
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.IsSuccess.ToString()),
                        ErrorCode = ErrorCode.IsSuccess,
                    };

                }

            }
            catch (Exception e)
            {
                return new Result()
                {
                    Message = e.Message,
                    ErrorCode = ErrorCode.SepasInternalServerError,
                    IsSuccess = false
                };
            }
        }

        public Result OrderComplete(string projectOrderId, ExternalProjectName projectName)
        {
            try
            {
                var bankService = _bankServiceFactory.Create("Baam");
                var order = _processOrderService.GetOrderByProjectOrderId(projectOrderId, projectName);
                if (order == null)
                    return new Result()
                    {
                        IsSuccess = false,
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.NoFoundRecord.ToString()),
                        ErrorCode = ErrorCode.NoFoundRecord,
                    };
                //سفارش بسته شده است
                if (order.Payable)
                    return new Result()
                    {
                        IsSuccess = true,
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.FinalizedOrderError.ToString()),
                        ErrorCode = ErrorCode.FinalizedOrderError,
                    };
                if (order.Status==CasStatuse.ProjectCancle)
                    return new Result()
                    {
                        IsSuccess = false,
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.OrderCancel.ToString()),
                        ErrorCode = ErrorCode.OrderCancel,
                    };
                //تاریخ بستن سفارش از duedate بزرگتر نباشد
                if (order.DueDate<=DateTime.Now)
                    return new Result()
                    {
                        IsSuccess = false,
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.CompleteDateError.ToString()),
                        ErrorCode = ErrorCode.CompleteDateError,
                    };

                var orderInquiryResult = OrderInquiry(projectOrderId, projectName);
                if (!orderInquiryResult.IsSuccess)
                {
                    return new Result()
                    {
                        IsSuccess = false,
                        Message = orderInquiryResult.Message,
                        ErrorCode = orderInquiryResult.ErrorCode
                    };
                }

                var checkParameterValue = _processOrderService.GetOrderSumAndCount(order);
                if (checkParameterValue.TotalAmount != orderInquiryResult.TotalAmount ||
                    checkParameterValue.TotalRecord != orderInquiryResult.TotalCount)
                {

                    return new Result()
                    {
                        IsSuccess = false,
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.NotSyncDataError.ToString()),
                        ErrorCode = ErrorCode.NotSyncDataError,
                    };

                }


                Func<Task<PaymentOrderRegisterOutput>> completePaymentFunc =
                    () => bankService.CompletePaymentOrder(order.Id.ToString());
                var completeOrderResult = RetryHelper.ExecuteWithRetry(completePaymentFunc, 3).Result;
                if (completeOrderResult.IsSuccess)
                {
                    order.Payable = true;
                    _processOrderService.UpdateOrderStatus(order, CasStatuse.Payable);
                    return new Result()
                    {
                        IsSuccess = true,
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.IsSuccess.ToString()),
                        ErrorCode = ErrorCode.IsSuccess,

                    };

                }

                return new Result()
                {
                    IsSuccess = false,
                    Message = completeOrderResult.Message,
                    ErrorCode = completeOrderResult.ErrorCode
                };

            }
            catch (DbUpdateException exception)
            {
                return new Result()
                {
                    Message = exception.Message,
                    ErrorCode = ErrorCode.SepasDatabaseError,
                    IsSuccess = false
                };

            }
            catch (Exception e)
            {
                return new Result()
                {
                    Message = e.Message,
                    ErrorCode = ErrorCode.SepasInternalServerError,
                    IsSuccess = false
                };
            }

        }
        public Result OrderConfirmation(string projectOrderId, ExternalProjectName projectName)
        {
            try
            {
                var bankService = _bankServiceFactory.Create("Baam");
                var order = _processOrderService.GetOrderByProjectOrderId(projectOrderId, projectName);
                if (order == null)
                    return new Result()
                    {
                        IsSuccess = false,
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.NoFoundRecord.ToString()),
                        ErrorCode = ErrorCode.NoFoundRecord,
                    };
                //سفارش بسته شده است
                if (!order.Payable)
                    return new Result()
                    {
                        IsSuccess =false,
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.UnFinalizedOrderError.ToString()),
                        ErrorCode = ErrorCode.UnFinalizedOrderError,
                    };
                if (order.Status==CasStatuse.ProjectCancle)
                    return new Result()
                    {
                        IsSuccess = false,
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.OrderCancel.ToString()),
                        ErrorCode = ErrorCode.OrderCancel,
                    };

                //قبلا تایید شده باشد
                if (order.Confirm)
                    return new Result()
                    {
                        IsSuccess = true,
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.ConfirmationIsDone.ToString()),
                        ErrorCode = ErrorCode.ConfirmationIsDone,
                    };

                Func<Task<TransactionConfirmationBindingModel>> confirmPaymentFunc =
                    () => bankService.TransactionConfirmationRequest(order.Id,new TransactionConfirmationBindingModel()
                    {
                        ConfirmationDateTime = DateTime.Now,
                        Confirmed=true,
                        TransactionId=order.Id.ToString()
                    });
                var completeOrderResult = RetryHelper.ExecuteWithRetry(confirmPaymentFunc, 3).Result;
                if (completeOrderResult.IsSuccess)
                {
                    //todo completeOrderResult.IsSuccess and code khata tekrari 
                    order.Confirm = true;
                    var update = _processOrderService.UpdateOrderStatus(order, CasStatuse.Confirm);
                    if (update == 1)
                    {
                        return new Result()
                        {
                            IsSuccess = true,
                            Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.IsSuccess.ToString()),
                            ErrorCode = ErrorCode.IsSuccess,

                        };
                    }
                    else
                    {
                        return new Result()
                        {
                            IsSuccess = false,
                            Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.SepasInternalServerError.ToString()),
                            ErrorCode = ErrorCode.SepasInternalServerError,

                        };
                    }
                  


                }

                return new Result()
                {
                    IsSuccess = false,
                    Message = completeOrderResult.Message,
                    ErrorCode = completeOrderResult.ErrorCode
                };

            }
            catch (DbUpdateException exception)
            {
                return new Result()
                {
                    Message = exception.Message,
                    ErrorCode = ErrorCode.SepasDatabaseError,
                    IsSuccess = false
                };

            }
            catch (Exception e)
            {
                return new Result()
                {
                    Message = e.Message,
                    ErrorCode = ErrorCode.SepasInternalServerError,
                    IsSuccess = false
                };
            }

        }

        private TResult GetFileFromServer<TResult>(string uriString) where TResult : class
        {
            var uri = new Uri(uriString);
            using (HttpClient clientGet = new HttpClient())
            {
                HttpResponseMessage response = clientGet.GetAsync(uri).Result;
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    //TODO : Log.Error(response.ReasonPhrase);
                    return default(TResult);
                }

                return JsonConvert.DeserializeObject<TResult>(
                    Encoding.UTF8.GetString(response.Content.ReadAsByteArrayAsync().Result));
            }
        }

        /// <summary>
        /// ارسال دستور اولیه برای ثبت یک سفارش
        /// </summary>
        /// <param name="orderData">داده های پردازش شده از فایل ها</param>
        /// <param name="orderId">شناسه سفارش</param>
        /// <param name="bankService">سرویس بانکی با توجه به نام بانک</param>
        /// <returns>BankGateway.Domain.Data.DTO.Results.Result.</returns>
        private Result RegisterOrder(TransferFileData orderData, Guid orderId, IBankService bankService)
        {
            Func<Task<PaymentOrderRegisterOutput>> registerOrderFunction = () =>
                bankService.RegisterOrder(
                    new PaymentOrderRegisterInput()
                    {
                        OrderId = orderId,
                        Payable = false,
                        SrcIban = orderData.SourceIBAN,
                        Title = orderData.SourceTitle,
                        Description = orderData.Description,
                        DueDate = orderData.DueDate
                    });
            return RetryHelper.ExecuteWithRetry(registerOrderFunction, 3).Result;
        }

        /// <summary>
        /// ارسال اطلاعات بسته ها به بانک
        /// </summary>
        /// <param name="package">بسته ثبت شده در بانک اطلاعاتی</param>
        /// <param name="bankService">سرویس بانکی با توجه به بانک هدف</param>
        /// <returns>BankGateway.Domain.Data.DTO.Results.Result.</returns>
        private Result SendPackageToBank(Package package, IBankService bankService)
        {

            try
            {
                Func<Task<BaamBatchTransactionOutput>> sendTransactionFunction = () =>
                        bankService.SendTransactionInformation(
                            new BaamBatchTransactionInput()
                            {
                                OrderId = package.OrderId,
                                PackageId = package.Id,
                                TransferInfos = package.Records.Select(x => new TransferInfo()
                                {
                                    OrderId = package.OrderId,
                                    Description = x.Description,
                                    RecordId = x.Id,
                                    Amount = Convert.ToDouble(x.Amount),
                                    DestinationIbanName = x.DestinationName,
                                    DestinationIban = x.DestinationShebaNo
                                }).ToList()
                            });

                var packageTransactionResult = RetryHelper.ExecuteWithRetry(sendTransactionFunction, 3).Result;
                //Result is Success
                if (packageTransactionResult.IsSuccess)
                {
                    return new Result()
                    {
                        IsSuccess = true,
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.IsSuccess.ToString()),
                        ErrorCode = ErrorCode.IsSuccess,
                    };
                }
                else
                {
                    return new Result()
                    {
                        IsSuccess = false,
                        Message = packageTransactionResult.Message,
                        ErrorCode = packageTransactionResult.ErrorCode,
                    };


                }


            }
            catch (DbUpdateException exception)
            {
                return new Result()
                {
                    Message = exception.Message,
                    ErrorCode = ErrorCode.SepasDatabaseError,
                    IsSuccess = false
                };
            }
            catch (HttpRequestException exception)
            {
                return new Result()
                {
                    Message = exception.Message,
                    ErrorCode = ErrorCode.SepasInternalServerError,
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                return new Result()
                {
                    Message = e.Message,
                    ErrorCode = ErrorCode.SepasInternalServerError,
                    IsSuccess = false
                };
            }

        }


        public Result PackageCancle(Guid projectPackageId, ExternalProjectName projectName)
        {
            try
            {
                var package = _packageService.GetPackageByProjectPackageId(projectPackageId, projectName);
                if (package != null)
                {
                    var update=_processOrderService.UpdatePackageStatus(package, CasStatuse.ProjectCancle);
                    if (update == 1)
                    {
                        _applicationServiceLogger.Info($"Package with Id : {package.Id} update Successfully to projectCancel");
                        return new Result()
                        {
                            IsSuccess = true,
                            Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.IsSuccess.ToString()),
                            ErrorCode = ErrorCode.IsSuccess,
                        };
                    }
                       
                    else
                    {
                        _applicationServiceLogger.Error($"Package with Id : {package.Id} can not update  to projectCancel"+ ErrorCode.SepasInternalServerError.ToString());
                        return new Result()
                        {
                            IsSuccess = false,
                            Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.SepasInternalServerError.ToString()),
                            ErrorCode = ErrorCode.SepasInternalServerError,
                        };
                    }
                }
                else
                {
                    _applicationServiceLogger.Info($"Package with Id : {package.Id} can not update  to projectCancel" + ErrorCode.PackageNotFound);

                    return new Result()
                    {
                        IsSuccess = false,
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.PackageNotFound.ToString()),
                        ErrorCode = ErrorCode.PackageNotFound,
                    };
                }
            }
            catch (DbUpdateException exception)
            {

                return new Result()
                {
                    Message = exception.Message,
                    ErrorCode = ErrorCode.SepasDatabaseError,
                    IsSuccess = false
                };
            }
            catch (HttpRequestException exception)
            {
                return new Result()
                {
                    Message = exception.Message,
                    ErrorCode = ErrorCode.SepasInternalServerError,
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                return new Result()
                {
                    Message = e.Message,
                    ErrorCode = ErrorCode.SepasInternalServerError,
                    IsSuccess = false
                };
            }
        }

        public Result OrderCancle(string projectOrderId, ExternalProjectName projectName)
        {
            try
            {
                var order = _processOrderService.GetOrderByProjectOrderId(projectOrderId, projectName);
                if (order != null)
                {
                    if (order.Status == CasStatuse.ProjectCancle)
                    {
                        return new Result()
                        {
                            IsSuccess = false,
                            Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.OrderCancel.ToString()),
                            ErrorCode = ErrorCode.OrderCancel,
                        };
                    }
                    if (!order.Confirm)
                    {
                        var update = _processOrderService.UpdateOrderStatus(order, CasStatuse.ProjectCancle);
                        if (update == 1)
                        {
                            _applicationServiceLogger.Info($"Package with Id : {order.Id} update Successfully to projectCancel");
                            return new Result()
                            {
                                IsSuccess = true,
                                Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.IsSuccess.ToString()),
                                ErrorCode = ErrorCode.IsSuccess,
                            };
                        }

                        else
                        {
                            _applicationServiceLogger.Error($"order with Id : {order.Id} can not update  to projectCancel" + ErrorCode.SepasInternalServerError.ToString());
                            return new Result()
                            {
                                IsSuccess = false,
                                Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.SepasInternalServerError.ToString()),
                                ErrorCode = ErrorCode.SepasInternalServerError,
                            };
                        }
                    }
                    else
                    {
                        return new Result()
                        {
                            IsSuccess = false,
                            Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.ConfirmationIsDone.ToString()),
                            ErrorCode = ErrorCode.ConfirmationIsDone,
                        };
                    }
                  
                }
                else
                {
                    _applicationServiceLogger.Info($"order with Id : {projectOrderId} can not update  to projectCancel" + ErrorCode.PackageNotFound);

                    return new Result()
                    {
                        IsSuccess = false,
                        Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.NoFoundRecord.ToString()),
                        ErrorCode = ErrorCode.NoFoundRecord,
                    };
                }
            }
            catch (DbUpdateException exception)
            {
                _applicationServiceLogger.Error($"order with Id : {projectOrderId} can not update  to projectCancel" ,exception);

                return new Result()
                {
                    Message = exception.Message,
                    ErrorCode = ErrorCode.SepasDatabaseError,
                    IsSuccess = false
                };
            }
            catch (HttpRequestException exception)
            {
                _applicationServiceLogger.Error($"order with Id : {projectOrderId} can not update  to projectCancel", exception);
                return new Result()
                {
                    Message = exception.Message,
                    ErrorCode = ErrorCode.SepasInternalServerError,
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                _applicationServiceLogger.Error($"order with Id : {projectOrderId} can not update  to projectCancel", e);
                return new Result()
                {
                    Message = e.Message,
                    ErrorCode = ErrorCode.SepasInternalServerError,
                    IsSuccess = false
                };
            }
        }
    }


}
