using System;

namespace BankGateway.Domain.Entities
{
    public class AppLog 
    {
        public DateTime? Date { get; set; }
        public string Thread { get; set; }
        public string Level { get; set; }
        public string Logger { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
        public int Id { get; set; }
    }
}