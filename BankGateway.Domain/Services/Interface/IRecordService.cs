using Core.Infrastructure.Common;

namespace BankGateway.Domain.Services.Interface
{
    using System;
    using System.Collections.Generic;
    using Entities;
    using Models.DTO.Results;
    using Models.Enum;

    public interface IRecordService
    {
        Record GetRecordByProjectRecordId(string projectRecordId, ExternalProjectName projectName);
        Result Update(Record record);

        void UpdateRecordsStatus(List<Record> records, PaymentStatus paymentStatus);
        Record GetRecord(Guid id);
        void Update(List<Record> records);
    }
}
