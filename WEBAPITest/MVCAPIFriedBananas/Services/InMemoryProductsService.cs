using System.Collections.Concurrent;
using MVCAPIFriedBananas.Models;

namespace MVCAPIFriedBananas.Services
{
    // Simple in-memory implementation for demo/testing
    public class InMemoryProductsService : IProductsService
    {
        private readonly ConcurrentDictionary<int, Product> _store = new();
        private int _nextId = 1;

        public Task<IEnumerable<Product>> GetAllAsync() =>
            Task.FromResult<IEnumerable<Product>>(_store.Values.OrderBy(p => p.ProdId));

        public Task<Product?> GetByIdAsync(int id) =>
            Task.FromResult(_store.TryGetValue(id, out var p) ? p : null);

        public Task<Product> CreateAsync(Product product)
        {
            var id = Interlocked.Increment(ref _nextId);
            product.ProdId = id;
            _store[id] = product;
            return Task.FromResult(product);
        }

        public Task<bool> UpdateAsync(Product product)
        {
            if (!_store.ContainsKey(product.ProdId)) return Task.FromResult(false);
            _store[product.ProdId] = product;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(int id) =>
            Task.FromResult(_store.TryRemove(id, out _));
    }
}
