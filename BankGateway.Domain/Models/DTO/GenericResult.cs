using BankGateway.Domain.Models.DTO.Results;

namespace BankGateway.Domain.Models.DTO
{
    public class GenericResult<T> : Result where T : class
    {
       
        public T ReturnObject { get; set; }
       
    }
}
