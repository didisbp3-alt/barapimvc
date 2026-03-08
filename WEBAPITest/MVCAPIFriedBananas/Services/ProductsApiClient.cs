using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using MVCAPIFriedBananas.Models;

namespace MVCAPIFriedBananas.Services
{
    public class ProductsApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductsApiClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient("BarEscolaApi");
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        private void AddAuthorizationHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        // ---------- Products ----------
        public async Task<List<Product>> GetProductsAsync()
        {
            AddAuthorizationHeader();
            return await _httpClient.GetFromJsonAsync<List<Product>>("api/Products") ?? new List<Product>();
        }

        public async Task<Product?> GetProductAsync(int id)
        {
            AddAuthorizationHeader();
            return await _httpClient.GetFromJsonAsync<Product>($"api/Products/{id}");
        }

        public async Task<Product?> CreateProductAsync(Product product)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.PostAsJsonAsync("api/Products", product);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<Product>();
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.PutAsJsonAsync($"api/Products/{product.ProdId}", product);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"api/Products/{id}");
            return response.IsSuccessStatusCode;
        }

        // ---------- Image Upload ----------
        public async Task<string?> UploadProductImageAsync(int productId, IFormFile file)
        {
            AddAuthorizationHeader();

            if (file == null || file.Length == 0)
                return null;

            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(file.OpenReadStream());
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(streamContent, "file", file.FileName);

            var response = await _httpClient.PostAsync($"api/ProductsAdmin/{productId}/upload-image", content);
            if (!response.IsSuccessStatusCode)
                return null;

            // The API returns { prodId, imgPath }
            var result = await response.Content.ReadFromJsonAsync<UploadImageResult>();
            return result?.imgPath;
        }

        private class UploadImageResult
        {
            public int prodId { get; set; }
            public string imgPath { get; set; }
        }
    }
}
