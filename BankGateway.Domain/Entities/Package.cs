
using BankGateway.Domain.Models.Enum;

namespace BankGateway.Domain.Entities
{
    using Core.Infrastructure.Common;
    using System.Collections.Generic;

    public partial class Package
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Package()
        {
            Records = new HashSet<Record>();
        }
        /// <summary>
        /// Id that generate by our system
        /// </summary>
        public System.Guid Id { get; set; }
      
        public System.Guid OrderId { get; set; }
        public System.DateTime CreationDate { get; set; }
        public CasStatuse Status { get; set; }

        public System.Guid ProjectPackageId { get; set; }
        public virtual Order Order { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Record> Records { get; set; }
    }
}
