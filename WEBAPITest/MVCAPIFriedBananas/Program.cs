using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MVCAPIFriedBananas.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// HttpClient for API + auth handler
builder.Services.AddHttpClient("BarEscolaApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:7234/");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
}).AddHttpMessageHandler<ApiAuthHandler>();

// ApiClients
builder.Services.AddScoped<CategoriesApiClient>();
builder.Services.AddScoped<ProductsApiClient>();
builder.Services.AddScoped<UsersApiClient>();
builder.Services.AddScoped<OrderApiClient>();
builder.Services.AddScoped<MenuApiClient>();
// builder.Services.AddScoped<BookingApiClient>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// JWT settings from appsettings
var authority = builder.Configuration["Jwt:Authority"];
var audience  = builder.Configuration["Jwt:Audience"];
var signingKey = builder.Configuration["Jwt:SigningKey"]; // optional if you validate a symmetric key

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = authority;
    options.Audience  = audience;
    options.RequireHttpsMetadata = false; // set true in production with HTTPS issuer

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        // If using symmetric key validation:
        IssuerSigningKey = !string.IsNullOrEmpty(signingKey)
            ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
            : null
    };

    // Pull token from session so MVC requests are authenticated
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            var token = ctx.HttpContext.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
                ctx.Token = token;
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Auth handler to add Bearer token on outgoing API calls
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
app.UseSession();          // session before auth so we can read the token
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();