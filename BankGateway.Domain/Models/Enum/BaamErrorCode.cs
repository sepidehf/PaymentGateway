using System.ComponentModel;

namespace BankGateway.Domain.Models.Enum
{
   public enum BaamErrorCode
    {
        [Description ("عملیات با موفقیت انجام شد")]
        Success=200,
        [Description("مشکل در توکن")]
        TockenError=401,

    }
}
