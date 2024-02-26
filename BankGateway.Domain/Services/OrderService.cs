

using Core.Infrastructure.Common;

namespace BankGateway.Domain.Services
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using EF;
    using Entities;
    using Helpers;
    using Models.BindingModel;
    using Models.DTO;
    using Models.Enum;
    using Interface;
    using Core.CrossCutting.Infrustructure.Logger;

    /// <summary>
    /// Class OrderService.
    /// </summary>
    /// <seealso cref="BankGateway.Domain.Services.Interface.IProcessOrderService" />
    public class OrderService : IProcessOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<Package> _packageRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<Record> _recordRepository;
        private readonly IRepository<Account> _accountRepository;
        private readonly ILogger _orderServiceLogger;

        public OrderService(IUnitOfWork unitOfWork)

        {
            _unitOfWork = unitOfWork;
            _orderServiceLogger = new Logger("OrderServiceLogger");
            _packageRepository = _unitOfWork.Repository<Package>();
            _orderRepository = _unitOfWork.Repository<Order>();
            _recordRepository = _unitOfWork.Repository<Record>();
            _accountRepository = _unitOfWork.Repository<Account>();
        }

        /// <summary>
        /// آماده سازی یک دستور پرداخت برای فایل های ورودی
        /// </summary>
        /// <param name="orderData">اطلاعات دستور پرداخت که با پردازش فایل ورودی بدست می آید.</param>
        /// <param name="ProjectPackageId">شناسه ای که برابر با آیدی فایل قابل پردازش است</param>
        /// <param name="projectName">نام پروژه ای که کار فراخوانی سرویس های ما را ارائه می دهد.</param>
        /// <returns>GenericResult&lt;PrepareOrderResultModel&gt;.</returns>
        public GenericResult<PrepareOrderResultModel> PrepareOrder(TransferFileData orderData, Guid ProjectPackageId, ExternalProjectName projectName)
        {

            try
            {
                var existingOrder = _orderRepository.GetBy(x => x.ProjectOrderId == orderData.ProjectOrderId
                                                                && x.ProjectName == projectName)
                    .FirstOrDefault();

                if (existingOrder != null)
                {
                    if (existingOrder.Status==CasStatuse.ProjectCancle)
                    {
                        _orderServiceLogger.Info(
                            $"order for project {projectName} with number {orderData.ProjectOrderId} " +
                            $", " + EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.OrderCancel.ToString()));
                        //Order is Payable and finalized later!
                        return new GenericResult<PrepareOrderResultModel>()
                        {
                            IsSuccess = false,
                            Message =
                                EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.OrderCancel.ToString()),
                            ErrorCode = ErrorCode.OrderCancel,
                            ReturnObject = null
                        };
                    }
                    if (existingOrder.Payable)
                    {
                        _orderServiceLogger.Info(
                            $"order for project {projectName} with number {orderData.ProjectOrderId} " +
                            $", " + EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.FinalizedOrderError.ToString()));
                        //Order is Payable and finalized later!
                        return new GenericResult<PrepareOrderResultModel>()
                        {
                            IsSuccess = false,
                            Message =
                                EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.FinalizedOrderError.ToString()),
                            ErrorCode = ErrorCode.FinalizedOrderError,
                            ReturnObject = null
                        };
                    }

                    var existingPackage = _packageRepository.GetBy(x => x.Order.ProjectOrderId == orderData.ProjectOrderId && x.ProjectPackageId == ProjectPackageId &&
                            x.Order.ProjectName == projectName)
                        .FirstOrDefault();
                    if (existingPackage != null)
                    {
                        _orderServiceLogger.Info(
                            $"Package for project {projectName} with number {orderData.ProjectOrderId} and package Id {ProjectPackageId}" +
                            $",casOrderId:{existingOrder.Id}and casPackageId{existingPackage.Id}"+ EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.DuplicatePackage.ToString()));
                        //Package exist for this order and package record must be updated
                        return new GenericResult<PrepareOrderResultModel>()
                        {
                            IsSuccess = false,
                            Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.DuplicatePackage.ToString()),
                            ErrorCode = ErrorCode.DuplicatePackage,
                            ReturnObject = null

                        };
                    }
                    else
                    {
                        //New Package For Order must be registered
                        var newPackage = new Package()
                        {
                            Id = Guid.NewGuid(),

                            CreationDate = DateTime.Now,
                            Status = CasStatuse.InsertToSepas,
                            ProjectPackageId = ProjectPackageId,
                            Order=existingOrder,
                            OrderId=existingOrder.Id
                            

                        };

                        _unitOfWork.BeginTransaction();

                        _packageRepository.Add(newPackage);
                        _unitOfWork.Save();

                        var records = PreparePackageRecords(orderData.PaymentList, newPackage.Id,projectName);
                        //Change Tracker off for bulk insert
                        _unitOfWork.AutoDetectChangesEnabled = false;

                        foreach (var record in records)
                        {
                            //newPackage.Records.Add(record);
                            record.PackageId = newPackage.Id;
                        }


                        _unitOfWork.CustomBulkInsert(records, option => option.BatchSize = 300);
                        //forInsertOrder.Packages.Add(newPackage);

                        _unitOfWork.Commit();


                        //newPackage.Order = existingOrder;
                        //newPackage.Records = PreparePackageRecords(orderData.PaymentList, newPackage.Id);
                        //_packageRepository.Add(newPackage);
                        //_unitOfWork.Save();
                        newPackage.Records = records;
                        _orderServiceLogger.Info(
                          $"Package for project {projectName} with ProjectOrder {orderData.ProjectOrderId} and packageOrder Id {ProjectPackageId}" +
                          $"CasOrderId:{existingOrder.Id} ,CasPackageId:{newPackage.Id}, Package Added" );
                        return new GenericResult<PrepareOrderResultModel>()
                        {
                            IsSuccess = true,
                            ReturnObject = new PrepareOrderResultModel()
                            {
                                ResultOrder = existingOrder,
                                ResultPackage = newPackage
                            }
                        };

                    }


                }
                else
                {
                    //////////////////////////////////////////////////////////////
                    //Create New Order With Full Component
                    //////////////////////////////////////////////////////////////
                    var forInsertOrder = new Order
                    {
                        Id = Guid.NewGuid(),
                        ProjectOrderId = orderData.ProjectOrderId,
                        CreateDate = DateTime.Now,
                        ProjectName = projectName,
                        Status = CasStatuse.InsertToSepas,
                        Payable = false,
                        Title = orderData.SourceTitle,

                        DueDate = DateTime.ParseExact(orderData.DueDate, "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
                    CultureInfo.CurrentCulture),

                        Description = orderData.Description,
                        Account = orderData.SourceIBAN == null
                            ? null
                            : _accountRepository.GetBy(x => x.IbanNo == orderData.SourceIBAN).FirstOrDefault()

                    };

                   _unitOfWork.BeginTransaction();
                    //////////////////////////////////////////////////////////////
                    //Add payment list to order 
                    //create one package for this order and
                    //add payment list to that as package record
                    //////////////////////////////////////////////////////////////
                    _orderRepository.Add(forInsertOrder);
                    _unitOfWork.Save();

                    var newPackage = new Package()
                    {
                        Id = Guid.NewGuid(),
                        Order = forInsertOrder,
                        //OrderId = forInsertOrder.Id,
                        CreationDate = DateTime.Now,
                        Status = CasStatuse.InsertToSepas,
                        ProjectPackageId = ProjectPackageId,
                    };

                    _packageRepository.Add(newPackage);
                    _unitOfWork.Save();

                    var records = PreparePackageRecords(orderData.PaymentList, newPackage.Id,projectName);
                    //Change Tracker off for bulk insert
                    _unitOfWork.AutoDetectChangesEnabled = false;

                    foreach (var record in records)
                    {
                        //newPackage.Records.Add(record);
                        record.PackageId = newPackage.Id;
                    }


                    _unitOfWork.CustomBulkInsert(records, option => option.BatchSize = 300);
                    //forInsertOrder.Packages.Add(newPackage);



                    _unitOfWork.Commit();
                    //_orderRepository.Add(forInsertOrder);
                    //Change Tracker ON after bulk insert


                    //var z = _unitOfWork.SaveChanges();
                    newPackage.Records = records;
                    _orderServiceLogger.Info(
                          $"Package for project {projectName} with number {orderData.ProjectOrderId} and package Id {ProjectPackageId}" +
                          $"Cas OrderId:{forInsertOrder.Id} and cas packageId:{newPackage.Id} ,New Order and package added");
                    return new GenericResult<PrepareOrderResultModel>()
                    {
                        IsSuccess = true,
                        ReturnObject = new PrepareOrderResultModel()
                        {
                            ResultOrder = forInsertOrder,
                            ResultPackage = newPackage
                        }
                    };
                }
            }

            catch (HttpRequestException exception)
            {
                _unitOfWork.Rollback();
                _orderServiceLogger.Debug($"HttpRequestException On {System.Reflection.MethodBase.GetCurrentMethod().Name}");
                return new GenericResult<PrepareOrderResultModel>()
                {
                    IsSuccess = false,
                    Message = exception.Message,
                    ErrorCode = ErrorCode.SepasInternalServerError

                };
            }
            catch (Exception exception)
            {
                _unitOfWork.Rollback();
                _orderServiceLogger.Debug($"Unknown Exception On {System.Reflection.MethodBase.GetCurrentMethod().Name}+{exception.Message}");
                return new GenericResult<PrepareOrderResultModel>()
                {
                    IsSuccess = false,
                    Message=exception.Message,
                    ErrorCode=ErrorCode.SepasInternalServerError
                  
                };
            }

        }

        /// <summary>
        /// جهت بروزرسانی وضعیت یک دستور پرداخت ازین تابع استفاده می شود
        /// </summary>
        /// <param name="targetOrder">دستور پرداخت هدف</param>
        /// <param name="targetPackage">بسته متناظر با دستور پرداخت هدف</param>
        /// <param name="targetStatus">وضعیتی که می تواند بروز رسانی شود <see cref="CasStatus" /></param>
        /// <returns>در صورت موفقیت آمیز بودن عدد 1 و در صورت شکست هر چیزی غیر از عدد 1 به عنوان خروجی بازگردانده می شود</returns>
        public int UpdateOrderStatus(Order targetOrder, Package targetPackage, CasStatuse targetStatus)
        {
            targetOrder.Status = targetStatus;
            targetPackage.Status = targetStatus;
            _orderRepository.Update(targetOrder);
            _packageRepository.Update(targetPackage);
            return _unitOfWork.Save();
        }

        public int UpdateOrderStatus(Order targetOrder, CasStatuse targetStatus)
        {
            targetOrder.Status = targetStatus;
            return _unitOfWork.Save();
        }
        public int UpdatePackageStatus(Package targetPackage, CasStatuse targetStatus)
        {
            targetPackage.Status = targetStatus;
            return _unitOfWork.Save();
        }

        public CheckParameterBindingModel GetOrderSumAndCount(Order order)
        {
            return _recordRepository.GetBy(x => x.Package.OrderId == order.Id && x.Package.Status!= CasStatuse.ProjectCancle)
                .GroupBy(i => 1).Select(r =>
                new CheckParameterBindingModel()
                {
                    TotalRecord = r.Count(),
                    TotalAmount = r.Sum(rec => rec.Amount)
                }).ToList().FirstOrDefault();

        }



        public List<Record> GetNotSentToBankRecords(Guid orderId)
        {
            return _recordRepository
                .GetBy(x => x.Package.OrderId == orderId &&
                            x.Package.Status == CasStatuse.InsertToSepas).ToList();

        }


        /// <summary>
        /// تابع کمکی برای انجام عملیات آماده سازی برای بسته های پرداخت به ازای هر دستور پرداخت
        /// </summary>
        /// <param name="packageRecords">رکوردهای هر بسته پس از پردازش اولیه</param>
        /// <param name="packageId"></param>
        /// <param name="paymentStatus">وضعیت پرداخت </param>
        /// <returns>ICollection&lt;Record&gt;.</returns>
        private ICollection<Record> PreparePackageRecords(List<PaymentInfo> packageRecords, Guid packageId, ExternalProjectName projectName, PaymentStatus paymentStatus = PaymentStatus.InsertToCas)
        {
            ICollection<Record> returnCollection = new List<Record>();
            foreach (var paymentInfo in packageRecords)
            {
                returnCollection.Add(new Record()
                {
                    Id = Guid.NewGuid(),
                    Amount = paymentInfo.Amount,
                    DateTime = DateTime.Now.ToString(CultureInfo.InvariantCulture),
                    Description = paymentInfo.Description,
                    PaymentCode = paymentInfo.PaymentId,
                    DestinationShebaNo = paymentInfo.DestinationIBAN,
                    DestinationName = paymentInfo.DestinationTitle,
                    PaymentStatus = paymentStatus,
                    ProjectRecordId = paymentInfo.ProjectRecordId,
                    ProjectName=projectName,
                    Account = string.IsNullOrWhiteSpace(paymentInfo.SourceIBAN) ? null : _accountRepository.GetBy(x => x.IbanNo == paymentInfo.SourceIBAN).FirstOrDefault()
                });
            }
            return returnCollection;
        }



        public Order GetOrderByProjectOrderId(string projectOrderId, ExternalProjectName projectName)
        {
            return _orderRepository.GetBy(x => x.ProjectOrderId == projectOrderId && x.ProjectName == projectName)
                .FirstOrDefault();
        }

        public Account GetAccountByIban(string iban)
        {
            return _accountRepository.GetBy(x => x.IbanNo == iban).FirstOrDefault();
        }


    }
}