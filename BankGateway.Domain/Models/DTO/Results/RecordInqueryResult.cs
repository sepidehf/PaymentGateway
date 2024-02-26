using System;

namespace BankGateway.Domain.Models.DTO.Results
{
   public class RecordInquiryResult : Result
    {
        public string ProjectRecordId { get; set; }
        //one of RECEIVED, SUBMITED, REGISTERED, PROCESSING,
        //SUCCEEDED, FAILED, CONTRADICTION, PARTIALY-SUCCEEDED,
        //PARTIALY-REGISTERED, CANCELED, SUSPENDED)
        public string Status { get; set; }
        public string DestinationIban { get; set; }
        public string Amount { get; set; }
        public string DEscription { get; set; }
        public string ProjectOrderId { get; set; }


    }
}
