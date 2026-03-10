using System.Net.Http.Headers;
using System.Net.Http.Json;
using DTO_MVCAPIContracts.Contracts;
using Microsoft.AspNetCore.Http;

namespace MVCAPIFriedBananas.Services;

public class MenuApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _ctx;

    public MenuApiClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClientFactory.CreateClient("BarEscolaApi");
        _ctx = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    private void AddAuthorizationHeader()
    {
        var token = _ctx.HttpContext?.Session.GetString("JwtToken");
        _httpClient.DefaultRequestHeaders.Authorization =
            string.IsNullOrEmpty(token) ? null : new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<List<MenusDto>> GetAllAsync()
    {
        AddAuthorizationHeader();
        var items = await _httpClient.GetFromJsonAsync<List<MenusDto>>("api/Menus");
        return items ?? new List<MenusDto>();
    }

    public async Task<MenusDto?> GetByIdAsync(int id)
    {
        AddAuthorizationHeader();
        return await _httpClient.GetFromJsonAsync<MenusDto>($"api/Menus/{id}");
    }

    public async Task<List<MenusDto>> GetRangeAsync(DateOnly start, DateOnly end)
    {
        AddAuthorizationHeader();
        var url   = $"api/Menus?start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}";
        var items = await _httpClient.GetFromJsonAsync<List<MenusDto>>(url);
        return items ?? new List<MenusDto>();
    }

    public async Task<LunchBookingDto?> BookAsync(int menuId)
    {
        AddAuthorizationHeader();
        var res = await _httpClient.PostAsJsonAsync("api/Booking", new { menuId });
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<LunchBookingDto>();
    }
}
