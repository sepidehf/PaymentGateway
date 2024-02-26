using System;
using Newtonsoft.Json;

namespace BankGateway.Domain.Models.DTO.BaamDTO
{

    /// <param name="Title">Order Title</param>
    /// <param name="Description">Description</param>
    /// <param name="DueDate">Transaction DateTime/ schaduled datetime</param>
    /// <param name="OrderId"> Order Identifire/unique id for all file that send with this order</param>
    /// <param name="SourceAccount"> </param>
    /// <param name="Payable">order register=false/ doing transaction=true </param>
    public class PaymentOrderRegisterInput
    {
        public PaymentOrderRegisterInput()
        {
            ConditionNumber = "01";
        }
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        /// <summary>
        /// تاریخ سررسید، روزی که پرداخت در آن انجام میشود
        /// </summary>
        [JsonProperty(PropertyName = "dueDate")]
        public string DueDate { get; set; }

      //  public DateTime AppDate { get; set; }


        [JsonProperty(PropertyName = "orderId")]
        public Guid OrderId { get; set; }

        [JsonProperty(PropertyName = "srcIBAN")]
        public string SrcIban { set; get; }

        [JsonProperty(PropertyName = "payable")]
        public bool Payable { get; set; }
        [JsonProperty(PropertyName = "conditionNumber")]
        public string ConditionNumber { get; set; }

    }
}
