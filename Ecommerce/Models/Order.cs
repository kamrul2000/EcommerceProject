using Ecommerce.Areas.Identity.Data;

namespace Ecommerce.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public EcommerceUser User { get; set; }

        public DateTime CreatedAt { get; set; }

       
        public string OrderStatus { get; set; } 
        public decimal TotalAmount { get; set; }
       public int AddressId { get; set; }
        public Address Address { get; set; }
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    }
}
