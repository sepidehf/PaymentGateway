
namespace BankGateway.Services.Wcf
{
    using System;
    using System.Runtime.Serialization;
    using System.ServiceModel;

    // NOTE: You can use the "Rename" command on the "Re-factor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IBankGatewayService
    {

        [OperationContract]
        FileTransactionOutput DoFileTransaction(FileTransactionInput serverFileGuid);

        [OperationContract]
        OrderCompleteOutput OrderComplete(OrderCompleteInput orderComplete);
        [OperationContract]
        RecordInquiryOutput RecordInquiry(RecordInquiryInput recordInquiry);

        [OperationContract]
        Result ResendOrderRecords(string orderId, string projectName);
    }
    [DataContract]
    public class FileTransactionInput
    {
        [DataMember]
        public Guid ServerFileId { get; set; }
        [DataMember]
        public string ProjectName { get; set; }
        [DataMember]
        public string UserName { get; set; }
    }

    [DataContract]
    public class FileTransactionOutput : Result
    {

        [DataMember]
        public Guid CasId { get; set; }

    }

    [DataContract]
    public class RecordInquiryInput
    {
        [DataMember]
        public Guid RecordId { get; set; }
        [DataMember]
        public string ProjectName { get; set; }
    }
    [DataContract]
    public class RecordInquiryOutput : Result
    {
        [DataMember]
        public string Status { get; set; }
        [DataMember]
        public Guid RecordId { get; set; }
    }
    [DataContract]
    public class OrderCompleteInput
    {

        [DataMember]
        public Guid OrderId { get; set; }
        [DataMember]
        public string ProjectName { get; set; }
    }
    [DataContract]
    public class OrderCompleteOutput : Result
    {

        [DataMember]
        public Guid OrderId { get; set; }

    }

    [DataContract]
    public class Result
    {
        [DataMember]
        public bool IsSuccess { get; set; }
        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public string ErrorCode { get; set; }
    }
}
