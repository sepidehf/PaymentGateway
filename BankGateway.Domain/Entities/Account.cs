using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankGateway.Domain.Entities
{
   public class Account
    {
       public Account()
       {
           this.Records=new List<Record>();
           this.Orders = new HashSet<Order>();
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public string AccountNo { get; set; }
        public string IbanNo { get; set; }
        public string BankName { get; set; }
        public ICollection<Record> Records { get; set; }
        public ICollection<Order> Orders { get; set; }
    }
}
