using System;
using System.Collections.Generic;
using System.Linq;

using BankGateway.Domain.EF;
using BankGateway.Domain.Entities;
using BankGateway.Domain.Helpers;
using BankGateway.Domain.Models.Enum;
using BankGateway.Domain.Services.Interface;
using Core.Infrastructure.Common;
using Result = BankGateway.Domain.Models.DTO.Results.Result;

namespace BankGateway.Domain.Services
{
    public class RecordService : IRecordService
    {
        private readonly IRepository<Record> _recordRepository;
        private readonly IUnitOfWork _unitOfWork;
        public RecordService(IUnitOfWork unitOfWork)
        {
            _recordRepository = unitOfWork.Repository<Record>();
            _unitOfWork = unitOfWork;
        }
        public Record GetRecordByProjectRecordId(string projectRecordId, ExternalProjectName projectName)
        {
            return
                _recordRepository.GetBy(
                    x => x.ProjectRecordId == projectRecordId && x.Package.Order.ProjectName == projectName)
                    .FirstOrDefault();
            
        }

                                 
        public Result Update(Record record)
        {
            try
            {
                _recordRepository.Update(record);
                _unitOfWork.Save();
                return new Result()
                {
                    Message = EnumHelper<ErrorCode>.GetEnumDescription(ErrorCode.IsSuccess.ToString()),
                    ErrorCode = ErrorCode.IsSuccess
                };
            }
            catch (Exception exception)
            {
                return new Result()
                {
                    Message = exception.Message,
                    ErrorCode = ErrorCode.SepasInternalServerError
                };
            }

        }

        public void UpdateRecordsStatus(List<Record> records, PaymentStatus paymentStatus)
        {
            foreach (var record in records)
            {
                record.PaymentStatus = paymentStatus;
            }

             _unitOfWork.CustomBulkUpdate(records);
        }

        public void Update(List<Record> records)
        {
            _unitOfWork.CustomBulkUpdate(records);
        }
        public Record GetRecord(Guid id)
        {
            return _recordRepository.GetBy(x => x.Id == id).FirstOrDefault();
        }
    }
}
