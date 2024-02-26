using BankGateway.Domain.Services.Interface;

namespace BankGateway.Domain.Factory
{
   public interface IBankServiceFactory
   {
       IBankService Create(string bankName);
   }
}
