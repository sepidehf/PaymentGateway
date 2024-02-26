﻿using System;
using System.Collections.Generic;
using BankGateway.Domain.Models.Enum;
using Core.Infrastructure.Common;


namespace BankGateway.Domain.Entities
{
    //------------------------------------------------------------------------------
    // <auto-generated>
    //     This code was generated from a template.
    //
    //     Manual changes to this file may cause unexpected behavior in your application.
    //     Manual changes to this file will be overwritten if the code is regenerated.
    // </auto-generated>
    //------------------------------------------------------------------------------


    public partial class Order
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Order()
        {
            Packages = new HashSet<Package>();
        }
        /// <summary>
        /// آی دی ای که خودمان می سازیم!
        /// </summary>
        /// <value>The identifier.</value>
        /// TODO Edit XML Comment Template for Id
        public System.Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public System.DateTime CreateDate { get; set; }
        public DateTime? DueDate { get; set; }
        public bool Payable { get; set; }
        public ExternalProjectName ProjectName { get; set; }
        public CasStatuse Status { get; set; }
        /// <summary>
        /// آی دی سفارشی که در همه فایل ها به ازای یک سفارش تکرا ر می شود.
        /// </summary>
        /// <value>The project order identifier.</value>
        /// TODO Edit XML Comment Template for ProjectOrderId
        public string ProjectOrderId { get; set; }
        public int SourceAccountId { get; set; }
        public bool Confirm { get; set; }
        public virtual Account Account { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Package> Packages { get; set; }

    }

}