using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO_MVCAPIContracts.Contracts
{
    public class UpdateCartItemRequestDTO
    {
        public int ProductId { get; set; }
        public int Qty { get; set; }
    }
}
