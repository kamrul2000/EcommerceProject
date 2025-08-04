using Ecommerce.Models;

namespace Ecommerce.ViewComponents
{
    public class OrderSummaryViewModel
    {
        public Order Order { get; set; }
        public Address Address { get; set; }

        public List<OrderItem> OrderItems { get; set; }
        public Address ShippingAddress { get; set; }
        public List<Cart> CartItems { get; set; }
        public decimal Total { get; set; }
    }
}
