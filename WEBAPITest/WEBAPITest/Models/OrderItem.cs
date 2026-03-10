using WEBAPITest.Models;

namespace WEBAPITest.Models
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public int? ProductId { get; set; }       // nullable if you add MenuId too
        public int? MenuId { get; set; }          // optional, if you support menus
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public Orders Order { get; set; } = null!;
        public Products? Product { get; set; }
        public Menu? Menu { get; set; }           // if using menus
    }
}
