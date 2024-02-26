using System;

namespace BankGateway.Domain.Models.DTO
{
    public class RecordDto
    {
        public Guid RecordId { get; set; }
        //public Guid PackageId { get; set; }
        public string SourceAccountNo { get; set; }
        public string SourceShebaNo { get; set; }
        public string DestinationAccountNo { get; set; }
        public string DestinationShebaNo { get; set; }
        public string SourceName { get; set; }
        public string DestinationName { get; set; }
        public string PaymentCode { get; set; }
        public string Description { get; set; }
        public double Amount { get; set; }
        public DateTime DateTime { get; set; }
        public string ProjectName { get; set; }


    }
}
