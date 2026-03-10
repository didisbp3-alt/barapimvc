//BookingApiClient.cs
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DTO_MVCAPIContracts.Contracts;
using Microsoft.AspNetCore.Http;

namespace MVCAPIFriedBananas.Services;

public class BookingApiClient
{
    #region Dependências
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _ctx;

    public BookingApiClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
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

    #region Actions
    public async Task<(bool Success, string Message)> BookAsync(int menuId)
    {
        AddAuthorizationHeader();
        var response = await _httpClient.PostAsync($"api/LunchBookings/book/{menuId}", null);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) return (false, !string.IsNullOrWhiteSpace(content) ? content : response.ReasonPhrase ?? "Erro");
        return (true, "Marcação efetuada com sucesso.");
    }

    public async Task<(bool Success, string Message)> CancelAsync(int bookingId)
    {
        AddAuthorizationHeader();
        var response = await _httpClient.PostAsync($"api/LunchBookings/cancel/{bookingId}", null);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) return (false, !string.IsNullOrWhiteSpace(content) ? content : response.ReasonPhrase ?? "Erro");
        return (true, "Marcação cancelada com sucesso.");
    }

    public async Task<List<LunchBookingDto>> GetMyBookingsAsync()
    {
        AddAuthorizationHeader();
        var items = await _httpClient.GetFromJsonAsync<List<LunchBookingDto>>("api/LunchBookings/me");
        return items ?? new List<LunchBookingDto>();
    }
    #endregion
}