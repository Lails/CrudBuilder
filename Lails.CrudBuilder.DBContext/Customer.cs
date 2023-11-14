using System.ComponentModel.DataAnnotations;

namespace Lails.CrudBuilder.DBContext
{
    public class Customer
    {
        [Key]
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }

        public List<Invoice> Invoices { get; set; }
    }
}
