using BankGateway.Domain.Models.Enum;
using Core.Infrastructure.Common;

namespace BankGateway.Domain.Models.DTO.Results
{
    public class Result
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public ErrorCode ErrorCode { get; set; }
    }
}
