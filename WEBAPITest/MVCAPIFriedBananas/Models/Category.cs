namespace MVCAPIFriedBananas.Models
{
    public class Category
    {
        public int CatId { get; set; }
        public string Name{get; set;}

        public bool? IsActive { get; set; }

        public string ImgPath { get; set; }
    }
}
