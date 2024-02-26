using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BankGateway.Domain.Models.DTO
{
    public class TransferFileData
    {
        /// <summary>
        /// Id that come from every project
        /// </summary>
     
        public string ProjectOrderId { get; set; }
        public string Description { get; set; }
        public decimal TotalAmount { get; set; }
        public string SourceTitle { get; set; }
        public string SourceIBAN { get; set; }
        public string DueDate { get; set; }
        public List<PaymentInfo> PaymentList { get; set; }

    }
}