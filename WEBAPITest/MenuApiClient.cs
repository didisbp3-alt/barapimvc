using System.Net.Http.Headers;
using System.Net.Http.Json;
using DTO_MVCAPIContracts.Contracts;
using Microsoft.AspNetCore.Http;

namespace MVCAPIFriedBananas.Services;

public class MenuApiClient
{
    #region Dependências
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _ctx;

    public MenuApiClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClientFactory.CreateClient("BarEscolaApi");
        _ctx = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }
    #endregion

    #region Helpers
    private void AddAuthorizationHeader()
    {
        var token = _ctx.HttpContext?.Session.GetString("JwtToken");
        _httpClient.DefaultRequestHeaders.Authorization =
            string.IsNullOrEmpty(token) ? null : new AuthenticationHeaderValue("Bearer", token);
    }
    #endregion

    #region Read
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
    #endregion
}