using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using WEBAPITest.Data;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// 1. CORS – ajusta a origem se a porta do MVC mudar
builder.Services.AddCors(options =>
{
    options.AddPolicy("_myAllowSpecificOrigins",
        policy => policy.WithOrigins("https://localhost:7223") // Porta do teu projeto MVC
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
});

// 2. Controllers + Swagger com JWT support
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SchoolBar API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT Auth → Escreve: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

// 3. DbContext – logs úteis em dev
builder.Services.AddDbContext<diogoportela_SchoolBarContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .EnableSensitiveDataLogging()           // dev only – remove em prod
           .EnableDetailedErrors()                 // dev only
           .LogTo(Console.WriteLine, LogLevel.Information));

// 4. JWT Authentication – sem fallback na chave!
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer não configurado"),
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience não configurado"),
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]
                    ?? throw new InvalidOperationException("Jwt:Key não configurada")))
        };

        // Ajuda a depurar falhas de autenticação (opcional – remove em prod)
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("JWT falhou: " + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("JWT validado com sucesso para user: " + context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }
        };
    });

// Políticas de autorização por nível de papel
builder.Services.AddAuthorization(options =>
{
    // Apenas Administradores (0)
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim(ClaimTypes.Role, "0"));
    // Bar/Cantina (1) OU Administrador (0)
    options.AddPolicy("BarOrAdmin", policy =>
        policy.RequireClaim(ClaimTypes.Role, "0", "1"));
});

// Registo dos serviços de domínio
builder.Services.AddScoped<WEBAPITest.Services.ILunchBookingService, WEBAPITest.Services.LunchBookingService>();
builder.Services.AddScoped<WEBAPITest.Services.IOrdersService, WEBAPITest.Services.OrdersService>();

var app = builder.Build();

// Pipeline
app.UseCors("_myAllowSpecificOrigins");

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware de debug temporário (remove depois de depurar)
app.Use(async (context, next) =>
{
    if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
    {
        Console.WriteLine($"Authorization header recebido: {authHeader}");
    }
    else
    {
        Console.WriteLine("Nenhum Authorization header no request");
    }
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();