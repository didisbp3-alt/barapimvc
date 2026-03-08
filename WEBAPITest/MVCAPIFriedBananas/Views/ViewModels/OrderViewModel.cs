namespace MVCAPIFriedBananas.Views.ViewModels
{
    public class OrderViewModel
    {
        public IReadOnlyList<OrderItemViewModel> Items { get; set; } = new List<OrderItemViewModel>();
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        public int ItemCount => Items.Sum(i => i.Quantity);
    }
}
