using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using MVCAPIFriedBananas.Models;

namespace MVCAPIFriedBananas.Services;

public class ProductsApiClient
{
    #region Dependências
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ProductsApiClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClientFactory.CreateClient("BarEscolaApi");
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }
    #endregion

    #region Helpers
    private void AddAuthorizationHeader()
    {
        var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
        if (!string.IsNullOrEmpty(token))
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        else
            _httpClient.DefaultRequestHeaders.Authorization = null;
    }
    #endregion

    #region Read
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
    #endregion

    #region Write
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
    #endregion
}