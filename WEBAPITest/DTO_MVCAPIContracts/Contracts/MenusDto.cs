using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO_MVCAPIContracts.Contracts
{
    public class MenusDto
    {
        public int MId { get; set; }
        public DateTime? Date { get; set; }
        public bool? Type { get; set; }
        public string MainDish { get; set; } = string.Empty;
        public string Soup { get; set; } = string.Empty;
        public string Dessert { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public int? MaxSeats { get; set; }
        public int? UsedSeats { get; set; }
        public int AvailableSeats { get; set; }   // required
    }
}
