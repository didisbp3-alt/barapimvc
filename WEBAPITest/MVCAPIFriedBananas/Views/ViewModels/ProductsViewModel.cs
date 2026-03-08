using Microsoft.AspNetCore.Http;

namespace MVCAPIFriedBananas.Views.ViewModels
{
    public class ProductsViewModel
    {
        public int ProdId { get; set; }
        public int? Cat_Id { get; set; }
        public string? CategoryName { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public int? DiscountPercent { get; set; }
        public int? StockCurrent { get; set; }
        public int? StockMax { get; set; }
        public bool? IsFavorite { get; set; }
        public string? ImageUrl { get; set; }
        public int ProductType { get; set; }
        /// <summary>Used only on Edit/Create forms for image upload; not persisted directly.</summary>
        public IFormFile? ImageFile { get; set; }
    }
}
