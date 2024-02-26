using System;
using System.Collections.Generic;

namespace BankGateway.Domain.Models.DTO
{
   public class OrderInfoDto
    {
       public OrderInfoDto()
       {
           this.PackageInfoes=new List<PackageInfoDto>();
       }
        public Guid OrderId { get; set; }
        public List<PackageInfoDto> PackageInfoes { get; set; } 
    }
}
