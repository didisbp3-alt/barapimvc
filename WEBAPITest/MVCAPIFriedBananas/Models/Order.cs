namespace MVCAPIFriedBananas.Models
{
    public class Order
    {
        public int OrderId { get; set; }

        public decimal Subtotal { get; set; }

        public decimal? Total { get; set; }

        public string? Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? Notes { get; set; }

        public List<OrderItem> Items { get; set; } = new();
    }
}
