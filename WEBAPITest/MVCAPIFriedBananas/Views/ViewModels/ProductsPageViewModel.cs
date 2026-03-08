using MVCAPIFriedBananas.Models;

namespace MVCAPIFriedBananas.Views.ViewModels
{
    public class ProductsPageViewModel
    {
        public IEnumerable<ProductsViewModel> Items { get; set; } = Enumerable.Empty<ProductsViewModel>();
        public IEnumerable<string> Categories { get; set; } = Enumerable.Empty<string>();

        public List<Category> CategoryList { get; set; } = new();

        public IEnumerable<string> Allergens { get; set; } = Enumerable.Empty<string>();
        public string? SelectedCategory { get; set; }
        public string? SelectedAllergen { get; set; }
        public string? PriceRange { get; set; }   // "", "lt5", "5to10", "gt10"
        public string? Search { get; set; }
        public int CartCount { get; set; }
    }
}
