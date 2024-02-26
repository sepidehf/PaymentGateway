using BankGateway.Domain.Entities;

namespace BankGateway.Domain.Models.BindingModel
{
    public class PrepareOrderResultModel
    {
        public Order ResultOrder { get; set; }
        public Package ResultPackage { get; set; }
    }
}