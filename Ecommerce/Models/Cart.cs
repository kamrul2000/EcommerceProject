using Ecommerce.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;

namespace Ecommerce.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public int ProductId { get; set; }      
        public Product Product { get; set; }    
        public int Qty { get; set; }
        public string UserId { get; set; } 
        public EcommerceUser User { get; set; } 
    }
}
