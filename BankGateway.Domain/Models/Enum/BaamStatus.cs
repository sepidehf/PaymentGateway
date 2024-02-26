using System.ComponentModel;

namespace BankGateway.Domain.Models.Enum
{
    public enum BaamStatus
    {
        //one of RECEIVED, SUBMITED, REGISTERED,
        //PROCESSING, SUCCEEDED, FAILED, CONTRADICTION,
        //PARTIALY-SUCCEEDED, PARTIALY-REGISTERED, CANCELED, SUSPENDED)
        All = 0,
        [Description("SUCCEEDED")]
        Succeeded = 1,
        Failed = 2,
        Suspended = 3,
        Canceled = 4,
        Received = 5,
        Submitted = 6,
        Registered = 7,
        Processing = 8,
        Contradiction = 9,
        PartiallySucceeded = 10,
        PartiallyRegistered = 11


    }
}
