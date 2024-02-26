using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BankGateway.Domain.Entities;
using Core.Infrastructure.Common;

namespace BankGateway.Domain.Services.Interface
{
   public interface IPackageService
   {
       Package GetPackageByProjectPackageId(Guid projectPackageId, ExternalProjectName projectName);
   }
}
