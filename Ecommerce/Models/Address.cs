using Ecommerce.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;

namespace Ecommerce.Models
{
    public class Address
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public EcommerceUser User { get; set; }
        public string? AddressLine { get; set; }
        public string? City { get; set; }
        public string AddressDescription { get; set; }
        public string Upzila { get; set; }
        public string District { get; set; }
        public string? State { get; set; }


        public string? Country { get; set; } 
        public string CustomerName { get; set; } 
        public decimal Phone { get; set; }
        public string Email { get; set; }




    }
}
