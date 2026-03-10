using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MVCAPIFriedBananas.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient("BarEscolaApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:7234/");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
}).AddHttpMessageHandler<ApiAuthHandler>();

builder.Services.AddScoped<CategoriesApiClient>();
builder.Services.AddScoped<ProductsApiClient>();
builder.Services.AddScoped<UsersApiClient>();
builder.Services.AddScoped<OrderApiClient>();
builder.Services.AddScoped<MenuApiClient>();
builder.Services.AddScoped<BookingApiClient>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var validIssuer   = builder.Configuration["Jwt:Authority"]
    ?? throw new InvalidOperationException("Jwt:Authority not configured");
var validAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience not configured");
var signingKey    = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Jwt:SigningKey not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme              = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme  = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme     = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = validIssuer,
        ValidAudience            = validAudience,
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            var token = ctx.HttpContext.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
                ctx.Token = token;
            return Task.CompletedTask;
        },
        OnChallenge = ctx =>
        {
            var isAjax = ctx.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                         (ctx.Request.Headers["Accept"].ToString().Contains("application/json"));
            if (!isAjax && !ctx.Response.HasStarted)
            {
                ctx.HandleResponse();
                var returnUrl = Uri.EscapeDataString(ctx.Request.Path + ctx.Request.QueryString);
                ctx.Response.Redirect($"/Auth/Login?returnUrl={returnUrl}");
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim(System.Security.Claims.ClaimTypes.Role, "0"));
});
builder.Services.AddTransient<ApiAuthHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
