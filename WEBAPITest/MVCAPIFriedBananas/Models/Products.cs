namespace MVCAPIFriedBananas.Models
{
    public class Product
    {
        public int ProdId { get; set; }
        public int CatId { get; set; }
        public string? CategoryName { get; set; }   // add this
        public string Name { get; set; }
        public string Description { get; set; }
        public int DiscountPercent { get; set; }
        public int MaxStock { get; set; }
        public int CurStock { get; set; }
        public int Kcal { get; set; }
        public decimal Protein { get; set; }
        public decimal Fat { get; set; }
        public decimal Carbs { get; set; }
        public decimal Salt { get; set; }
        public string Allergens { get; set; }
        public string ImgPath { get; set; }
        public decimal? Price { get; set; }

        public int ProductType { get; set; }
        public bool? IsFavorite { get; set; }        
        public bool? IsActive { get; set; }
    }
}
