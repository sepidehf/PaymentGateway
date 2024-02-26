using System.ComponentModel;

namespace BankGateway.Domain.Models.Enum
{
    public enum BaamScope
    {
        None,
        [Description("transaction")]
        Transaction,
        [Description("account")]
        Account,
        [Description("money-transfer")]
        MoneyTransfer,

        [Description("transaction,account,money-transfer")]
        AllScope,



    }
}