using DTO_MVCAPIContracts.Contracts;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

public class OrderApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _ctx;
    private const string OrderGetRoute = "api/Orders/cart";
    private const string OrderAddRoute = "api/Orders/add";
    private const string OrderUpdateRoute = "api/Orders/update";
    private const string OrderRemoveRoute = "api/Orders/remove";

    public OrderApiClient(IHttpClientFactory f, IHttpContextAccessor ctx)
    {
        _http = f.CreateClient("BarEscolaApi");
        _ctx = ctx;
    }

    private void Auth()
    {
        var token = _ctx.HttpContext?.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
            throw new InvalidOperationException("JWT token not found in session.");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<OrderDto> GetCartAsync()
    {
        Auth();
        var res = await _http.GetAsync(OrderGetRoute);

        if (res.StatusCode == HttpStatusCode.NotFound ||
            res.StatusCode == HttpStatusCode.Unauthorized ||
            res.StatusCode == HttpStatusCode.Forbidden)
        {
            return new OrderDto { Items = Array.Empty<OrderItemDto>(), Subtotal = 0m, Total = 0m };
        }

        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Cart API failed: {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");
        }

        return await res.Content.ReadFromJsonAsync<OrderDto>()
               ?? new OrderDto { Items = Array.Empty<OrderItemDto>(), Subtotal = 0m, Total = 0m };
    }

    public async Task AddItemAsync(int productId, int qty = 1)
    {
        Auth();
        var res = await _http.PostAsJsonAsync(OrderAddRoute, new { productId, qty });
        res.EnsureSuccessStatusCode();
    }

    public async Task UpdateItemAsync(int productId, int qty)
    {
        Auth();
        var res = await _http.PostAsJsonAsync(OrderUpdateRoute, new { productId, qty });
        res.EnsureSuccessStatusCode();
    }

    public async Task RemoveItemAsync(int productId)
    {
        Auth();
        var res = await _http.PostAsJsonAsync(OrderRemoveRoute, new { productId });
        res.EnsureSuccessStatusCode();
    }
}
