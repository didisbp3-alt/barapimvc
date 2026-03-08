using Microsoft.AspNetCore.Http;
using MVCAPIFriedBananas.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MVCAPIFriedBananas.Services
{
    public class CategoriesApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CategoriesApiClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient("BarEscolaApi");
            _httpContextAccessor = httpContextAccessor;
        }

        private void AddAuthorizationHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            AddAuthorizationHeader();
            return await _httpClient.GetFromJsonAsync<List<Category>>("api/Categories")
                   ?? new List<Category>();
        }

        public async Task<Category?> GetCategoryAsync(int id)
        {
            AddAuthorizationHeader();
            return await _httpClient.GetFromJsonAsync<Category>($"api/Categories/{id}");
        }

        public async Task<bool> CreateCategoryAsync(Category category)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.PostAsJsonAsync("api/Categories", category);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateCategoryAsync(Category category)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.PutAsJsonAsync($"api/Categories/{category.CatId}", category);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"api/Categories/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}