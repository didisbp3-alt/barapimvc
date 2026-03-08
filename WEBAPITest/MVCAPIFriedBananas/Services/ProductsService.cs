using System.Collections.Generic;
using System.Threading.Tasks;
using MVCAPIFriedBananas.Models;

namespace MVCAPIFriedBananas.Services
{
    public interface IProductsService
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);
        Task<Product> CreateAsync(Product product);
        Task<bool> UpdateAsync(Product product);
        Task<bool> DeleteAsync(int id);
    }
}
