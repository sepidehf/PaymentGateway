using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BankGateway.Domain.EF;
using BankGateway.Domain.Entities;
using BankGateway.Domain.Services.Interface;
using Core.Infrastructure.Common;

namespace BankGateway.Domain.Services
{
  public  class PackageService:IPackageService
    {
        private readonly IRepository<Package> _packageRepository;
        private readonly IUnitOfWork _unitOfWork;
        public PackageService(IUnitOfWork unitOfWork)
        {
            _packageRepository = unitOfWork.Repository<Package>();
            _unitOfWork = unitOfWork;
        }

    

        public Package GetPackageByProjectPackageId(Guid projectPackageId, ExternalProjectName projectName)
        {
                var package=
                _packageRepository.GetBy(
                    x => x.ProjectPackageId == projectPackageId && x.Order.ProjectName == projectName).ToList();
            if (package.Count == 1)
                return package.FirstOrDefault();
            return null;
        }   
    }
}
