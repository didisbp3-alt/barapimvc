using System.ComponentModel.DataAnnotations;

namespace MVCAPIFriedBananas.Models
{
    public class OrderItem
    {
        [Required]
        public int ProductId { get; set; }

        public string? Name { get; set; }

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }
    }
}
