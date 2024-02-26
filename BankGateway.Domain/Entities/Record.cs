


using Core.Infrastructure.Common;

namespace BankGateway.Domain.Entities
{
    using System;
    using Models.Enum;
    public partial class Record
    {
        public Guid Id { get; set; }
        public string ProjectRecordId { get; set; }
        public Guid PackageId { get; set; }
        public int? SourceAccountId { get; set; }
        public string DestinationAccountNo { get; set; }
        public string DestinationShebaNo { get; set; }
        public string SourceName { get; set; }
        public string DestinationName { get; set; }
        public string PaymentCode { get; set; }
        public string Description { get; set; }
        public double Amount { get; set; }
        public string DateTime { get; set; }
        public ExternalProjectName ProjectName { get; set; }
        public PaymentStatus PaymentStatus { get; set; }

        public virtual Package Package { get; set; }
        public virtual Account Account { get; set; }
    }
}