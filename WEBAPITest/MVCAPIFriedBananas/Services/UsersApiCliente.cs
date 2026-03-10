using Microsoft.AspNetCore.Http;
using MVCAPIFriedBananas.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MVCAPIFriedBananas.Services
{
    public class UsersApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UsersApiClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient("BarEscolaApi");
            _httpContextAccessor = httpContextAccessor;
        }

        private void AddAuthorizationHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<CurrentUserDto?> GetCurrentUserAsync()
        {
            AddAuthorizationHeader();
            var response = await _httpClient.GetAsync("api/Users/me");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CurrentUserDto>();
        }

        public async Task<List<UserAdminDto>> GetAllUsersAsync()
        {
            AddAuthorizationHeader();
            var response = await _httpClient.GetAsync("api/Users");
            if (!response.IsSuccessStatusCode) return new List<UserAdminDto>();
            return await response.Content.ReadFromJsonAsync<List<UserAdminDto>>() ?? new List<UserAdminDto>();
        }

        public async Task<UserAdminDto?> GetUserByIdAsync(int id)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/Users/{id}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<UserAdminDto>();
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"api/Users/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}