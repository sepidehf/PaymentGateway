using System;
using BankGateway.Domain.Models.Enum;
using Core.Infrastructure.Common;

namespace BankGateway.Domain.Models.DTO
{
    public class FileTransactionInput
    {
        public Guid ServerFileId { get; set; }
     
        public ExternalProjectName  ProjectName { get; set; }
        public string Username { get; set; }
    }
}
