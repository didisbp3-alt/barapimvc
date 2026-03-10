using System;
namespace MVCAPIFriedBananas.Models
{
    public class Menu
    {
        public int MId { get; set; }
        public int CatId { get; set; }
        public DateTime Date { get; set; }
        public bool Type {  get; set; }
        public string MainDish { get; set; }
        public string Soup { get; set; }
        public string Dessert { get; set; }
        public string Notes { get; set; }
        public int MaxSeats { get; set; }
        public int MinSeats { get; set; }
        public int AvailableSeats { get; set; }
    }
}
