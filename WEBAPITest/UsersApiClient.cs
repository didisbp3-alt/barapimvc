using Microsoft.AspNetCore.Http;
using MVCAPIFriedBananas.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MVCAPIFriedBananas.Services;

public class UsersApiClient
{
    #region Dependências
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UsersApiClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
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

    #region Users
    public async Task<CurrentUserDto?> GetCurrentUserAsync()
    {
        AddAuthorizationHeader();
        var response = await _httpClient.GetAsync("api/Users/me");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<CurrentUserDto>();
    }
    #endregion
}