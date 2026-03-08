# BarEscola — School Bar & Canteen Web Platform

A full-stack school bar/canteen management system built with **ASP.NET Core MVC** (front-end) and a separate **ASP.NET Core Web API** back-end. Users can browse products, manage a shopping cart, view daily menus and book lunch, while admins manage the full product/category/menu catalogue.

---

## Table of Contents

1. [Solution Structure](#solution-structure)
2. [Architecture Overview](#architecture-overview)
3. [Authentication & JWT](#authentication--jwt)
4. [Session Management](#session-management)
5. [HTTP Client Pattern (ApiClients)](#http-client-pattern-apiclients)
6. [Controllers](#controllers)
7. [Services (ApiClients)](#services-apiclients)
8. [Models & ViewModels](#models--viewmodels)
9. [Cart System](#cart-system)
10. [Products & Categories](#products--categories)
11. [Menus & Lunch Bookings](#menus--lunch-bookings)
12. [Favorites](#favorites)
13. [Views & UI](#views--ui)
14. [CSS & Theming](#css--theming)
15. [API Project (WEBAPITest)](#api-project-webapitest)
16. [Configuration](#configuration)
17. [Running the Project](#running-the-project)

---

## Solution Structure

```
WEBAPITest/
├── WEBAPITest/                    ← ASP.NET Core Web API (back-end)
│   ├── Controllers/               ← API endpoints (Products, Categories, Auth, Orders, Menus, LunchBookings, Users)
│   ├── Data/                      ← Entity Framework DbContext (SQL Server)
│   ├── Models/                    ← EF entity classes (Products, Category, Menu, Orders, Users, LunchBooking)
│   ├── Services/                  ← Business logic (OrdersService, LunchBookingService)
│   └── Program.cs                 ← API startup: CORS, JWT, EF, Swagger
│
└── MVCAPIFriedBananas/            ← ASP.NET Core MVC (front-end)
    ├── Controllers/               ← MVC controllers
    ├── Models/                    ← Client-side models (LoginModel, RegisterModel, Product, etc.)
    ├── Services/                  ← ApiClient classes (HTTP wrappers)
    ├── Views/                     ← Razor views (.cshtml)
    │   ├── Auth/                  ← Login, Register
    │   ├── Products/              ← Index, AdminIndex, Create, Edit, Delete, Details, Highlights
    │   ├── Categories/            ← Index, Create, Edit, Delete
    │   ├── Cart/                  ← Index (cart view)
    │   ├── Menu/                  ← Index (weekly menu), admin views
    │   ├── Booking/               ← Index (my bookings)
    │   ├── Users/                 ← Details, Edit
    │   └── Shared/                ← _Layout, _ProductCard, _UserMenu
    ├── Views/ViewModels/          ← ViewModel classes used by views
    ├── wwwroot/                   ← Static assets (CSS, JS, images)
    │   └── css/
    │       ├── site.css           ← Global design system (variables, header, footer, buttons)
    │       ├── products.css       ← Product listing and card styles
    │       └── login.css          ← Login/Register page styles
    └── Program.cs                 ← MVC startup: services, DI, JWT, session, routing
```

---

## Architecture Overview

The solution is split into two independently running projects:

| Layer | Project | Port |
|---|---|---|
| **Web API** | `WEBAPITest` | `https://localhost:7234` |
| **MVC Front-end** | `MVCAPIFriedBananas` | `https://localhost:7223` |

The MVC project **never** talks to the database directly. It communicates exclusively with the Web API through typed `HttpClient` wrapper classes called **ApiClients**. Every HTTP call carries a JWT Bearer token read from the server-side session.

```
Browser → MVC Controller → ApiClient (HttpClient) → Web API → SQL Server (EF Core)
```

The `ApiAuthHandler` (a `DelegatingHandler`) automatically appends the `Authorization: Bearer <token>` header to every outgoing HTTP request, so individual ApiClient methods don't need to set it themselves — though they also call `AddAuthorizationHeader()` directly as a belt-and-suspenders approach.

---

## Authentication & JWT

### Login Flow

1. User submits email + password to `POST /Auth/Login`.
2. `AuthController.Login()` calls `POST api/Auth/login` on the Web API.
3. The API validates credentials, generates a JWT and returns `{ "token": "..." }`.
4. The MVC controller parses the JSON response with `JsonDocument` (handles both camelCase `token` and PascalCase `Token`):
   ```csharp
   if (root.TryGetProperty("token", out var p) || root.TryGetProperty("Token", out p))
       token = p.GetString();
   ```
5. The token is stored in the server-side session: `HttpContext.Session.SetString("JwtToken", token)`.
6. The user is redirected to the home page.

### JWT Validation in MVC

The MVC app is configured to validate JWT tokens. The `OnMessageReceived` event hook reads the token from the session and injects it into the JWT middleware pipeline:

```csharp
OnMessageReceived = ctx =>
{
    var token = ctx.HttpContext.Session.GetString("JwtToken");
    if (!string.IsNullOrEmpty(token))
        ctx.Token = token;
    return Task.CompletedTask;
}
```

This is why `[Authorize]` attributes on MVC controllers work even though the token is not in an HTTP header — the middleware reads it from the session before validating it.

### Redirect on 401

Instead of returning a 401 JSON response when an unauthenticated browser navigates to a protected page, the `OnChallenge` event redirects to `/Auth/Login`:

```csharp
OnChallenge = ctx =>
{
    var isAjax = ctx.Request.Headers["X-Requested-With"] == "XMLHttpRequest" || ...;
    if (!isAjax && !ctx.Response.HasStarted)
    {
        ctx.HandleResponse();
        ctx.Response.Redirect($"/Auth/Login?returnUrl={returnUrl}");
    }
    return Task.CompletedTask;
}
```

AJAX requests (fetch/XHR) still receive the HTTP 401, so the JS code can handle them without a page reload.

### JWT Settings

Configured in `appsettings.json`:
```json
{
  "Jwt": {
    "Authority": "api-escolar",
    "Audience":  "mvc-client",
    "SigningKey": "<secret key>"
  }
}
```

Both the MVC app and the Web API use the same `Authority`, `Audience`, and `SigningKey` so that tokens generated by the API are accepted by MVC's validation middleware.

---

## Session Management

ASP.NET Core's distributed in-memory session is used for two purposes:

| Key | Value | Purpose |
|---|---|---|
| `"JwtToken"` | JWT string | Persists the user's authentication token across requests |
| `"UserFavorites"` | JSON `HashSet<int>` | Stores product IDs that the user has favorited |

Session is configured in `Program.cs`:
```csharp
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout     = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
```

`UseSession()` is called **before** `UseAuthentication()` in the middleware pipeline so that the session is already loaded when the JWT `OnMessageReceived` event fires.

---

## HTTP Client Pattern (ApiClients)

All HTTP communication with the Web API goes through typed wrapper classes in `Services/`. Each inherits from nothing — they are plain classes registered with DI.

A single named `HttpClient` called `"BarEscolaApi"` is configured once in `Program.cs`:

```csharp
builder.Services.AddHttpClient("BarEscolaApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:7234/");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback =
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
}).AddHttpMessageHandler<ApiAuthHandler>();
```

`DangerousAcceptAnyServerCertificateValidator` skips TLS certificate validation — acceptable for local development but must be removed in production.

`ApiAuthHandler` is a `DelegatingHandler` that runs before every request:
```csharp
protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, ...)
{
    var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
    if (!string.IsNullOrEmpty(token))
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    return base.SendAsync(request, cancellationToken);
}
```

---

## Controllers

| Controller | Routes | Responsibility |
|---|---|---|
| `AuthController` | `/Auth/Login`, `/Auth/Register`, `/Auth/Logout` | Authentication — login, register, logout |
| `ProductsController` | `/Products/...` | Product listing, filtering, admin CRUD, favorites |
| `CategoriesController` | `/Categories/...` | Category admin CRUD |
| `CartController` | `/Cart/...` | Shopping cart view, add/update/remove items |
| `MenuController` | `/Menu/Index` | Weekly lunch menu display (Mon–Fri) |
| `BookingController` | `/Booking/...` | View and manage lunch bookings |
| `UsersController` | `/Users/...` | User profile view and edit |
| `HomeController` | `/Home/Index` | Landing page |

### Role-based Access

User roles are encoded as claims inside the JWT. Role `0` and `1` are staff/admin. Role checking in views uses:
```csharp
var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
bool isStaff = userRole == "0" || userRole == "1";
```

---

## Services (ApiClients)

### `ProductsApiClient`

Wraps `api/Products` and `api/ProductsAdmin/{id}/upload-image`.

| Method | HTTP | Endpoint |
|---|---|---|
| `GetProductsAsync()` | GET | `api/Products` |
| `GetProductAsync(id)` | GET | `api/Products/{id}` |
| `CreateProductAsync(product)` | POST | `api/Products` |
| `UpdateProductAsync(product)` | PUT | `api/Products/{id}` |
| `DeleteProductAsync(id)` | DELETE | `api/Products/{id}` |
| `UploadProductImageAsync(id, file)` | POST | `api/ProductsAdmin/{id}/upload-image` |

Image upload uses `MultipartFormDataContent` with a single `StreamContent` part named `"file"`.

### `CategoriesApiClient`

Wraps `api/Categories`. Full CRUD: `GetCategoriesAsync`, `GetCategoryAsync(id)`, `CreateCategoryAsync`, `UpdateCategoryAsync`, `DeleteCategoryAsync`.

### `OrderApiClient` (in `CartApiController.cs`)

Wraps the shopping cart endpoints.

| Method | HTTP | Endpoint |
|---|---|---|
| `GetCartAsync()` | GET | `api/Orders/cart` |
| `AddItemAsync(productId, qty)` | POST | `api/Orders/add` |
| `UpdateItemAsync(productId, qty)` | POST | `api/Orders/update` |
| `RemoveItemAsync(productId)` | POST | `api/Orders/remove` |

Returns an empty cart object (instead of throwing) when the user is unauthenticated or the cart is not found (404).

### `MenuApiClient`

Wraps `api/Menus`.

| Method | HTTP | Endpoint |
|---|---|---|
| `GetAllAsync()` | GET | `api/Menus` |
| `GetByIdAsync(id)` | GET | `api/Menus/{id}` |
| `GetRangeAsync(start, end)` | GET | `api/Menus?start=...&end=...` |

### `BookingApiClient`

Wraps `api/LunchBookings`.

| Method | HTTP | Endpoint |
|---|---|---|
| `BookAsync(menuId)` | POST | `api/LunchBookings/book/{menuId}` |
| `CancelAsync(bookingId)` | POST | `api/LunchBookings/cancel/{bookingId}` |
| `GetMyBookingsAsync()` | GET | `api/LunchBookings/me` |

### `UsersApiClient`

Wraps `api/Users/me` — returns the current authenticated user's profile as `CurrentUserDto`.

---

## Models & ViewModels

### Client Models (`Models/`)

These mirror the API's data transfer objects:

| Class | Fields |
|---|---|
| `Product` | `ProdId`, `CatId`, `Name`, `Description`, `Price`, `MaxStock`, `CurStock`, `DiscountPercent`, `Allergens`, `ImgPath`, `ProductType`, `IsFavorite`, `IsActive` |
| `Category` | `CatId`, `Name`, `IsActive`, `ImgPath` |
| `LoginModel` | `Email`, `Password` |
| `RegisterModel` | `FullName`, `Email`, `Password` |
| `LunchBooking` | Booking entity |
| `Menu` | Menu entity |

### ViewModels (`Views/ViewModels/`)

| ViewModel | Purpose |
|---|---|
| `ProductsViewModel` | Single product displayed in a card or form. Includes `ImageFile` (`IFormFile`) for upload. |
| `ProductsPageViewModel` | Products listing page. Holds filter state (`Search`, `SelectedCategory`, `SelectedAllergen`, `PriceRange`, `FavoritesOnly`) and the list of `Items`. |
| `WeeklyMenuViewModel` | Monday–Friday week view. Contains `WeekStart`, `Days` (list of `MenuDayViewModel`) and `BookedDates` (which days the user already booked). |
| `MenuDayViewModel` | One day: `Date`, `Normal` menu, `Vegetarian` menu. |
| `MenuItemViewModel` | One menu option: `MainDish`, `Soup`, `Dessert`, `MaxSeats`, `UsedSeats`, `AvailableSeats`. |
| `OrderViewModel` | Cart summary with `Items` and `Total`. |
| `OrderItemViewModel` | One cart line: `ProductId`, `Name`, `UnitPrice`, `Quantity`. |

---

## Cart System

The cart is server-side, stored in the Web API database and linked to the authenticated user. The MVC layer acts as a proxy.

### Add to Cart Flow

1. User clicks "Adicionar" → JS `fetch` POSTs to `POST /Cart/Add?id={productId}&qty=1`.
2. `CartController.Add()` calls `OrderApiClient.AddItemAsync(id, qty)` → `POST api/Orders/add`.
3. Returns JSON `{ success: true, cartCount: N }` for AJAX, or redirects for non-AJAX.
4. JS updates the cart badge counter in the navbar: `badge.textContent = json.cartCount`.

### Cart Badge Update

The `<span class="cart-count" data-cart-count>` element in the layout is updated client-side without a page reload. The navbar count is populated on page load from the server (see `_Layout.cshtml` cart link).

### CSRF Protection

All state-changing cart actions (`Add`, `Update`, `Remove`) have `[ValidateAntiForgeryToken]`. The antiforgery token is embedded in the page as a `<meta>` tag and read by JS:
```javascript
const token = document.querySelector('meta[name="request-verification-token"]')?.content;
```

---

## Products & Categories

### Product Listing (`/Products`)

1. `ProductsController.Index()` fetches all `ProductType == 0` products from the API.
2. `ApplyFavoritesToProducts()` marks each product as favorite if its `ProdId` is in the session-stored `HashSet<int>`.
3. `ApplyFilters()` applies all active filters (search, category, allergen, price range, favorites-only).
4. Each product is mapped to a `ProductsViewModel` via `ToCard()` — this also resolves the category name from the category list.
5. The view model is passed to `Index.cshtml` which renders the hero + filter bar + product grid.

### Filtering (AJAX)

The filter bar submits to `GET /Products/Filter` via `fetch`. The response is a partial HTML snippet (`_ProductCard.cshtml`) that replaces the product grid `innerHTML` without a full page reload.

### Admin CRUD

Admin pages require `[Authorize]`. The Edit and Create forms:
- Use a `<select>` populated from `ViewBag.Categories` (category names with `CatId` as values).
- Include `StockCurrent` and `StockMax` fields.
- Support image file upload (`enctype="multipart/form-data"`, `IFormFile ImageFile`).
- After saving, if `ImageFile` is present, `UploadProductImageAsync()` is called to upload the image to `api/ProductsAdmin/{id}/upload-image`.

---

## Menus & Lunch Bookings

### Weekly Menu (`/Menu`)

1. Calculates the Monday of the requested week (or the current week).
2. Clamps weekend inputs to the following Monday.
3. Builds exactly 5 `MenuDayViewModel` objects (Mon–Fri) using `Enumerable.Range(0, 5)`.
4. Loads the user's existing bookings for this week from `BookingApiClient.GetMyBookingsAsync()`.
5. `BookedDates` (a `HashSet<DateOnly>`) is passed to the view so already-booked days show a green "✓ Reservado" badge and their book buttons are disabled.

### Making a Booking

1. User clicks the book button → `POST /Booking/Book?menuId={id}&weekStart={date}`.
2. `BookingController.Book()` calls `BookingApiClient.BookAsync(menuId)` → `POST api/LunchBookings/book/{menuId}`.
3. The API enforces one booking per day per user.
4. Result message is stored in `TempData["BookingMessage"]` and displayed on redirect.

---

## Favorites

Favorites are stored entirely client-side in the server session (no database involvement).

```csharp
private const string FavSessionKey = "UserFavorites";

private HashSet<int> GetFavorites()
{
    var json = HttpContext.Session.GetString(FavSessionKey);
    if (string.IsNullOrEmpty(json)) return new HashSet<int>();
    return JsonSerializer.Deserialize<HashSet<int>>(json) ?? new HashSet<int>();
}

private void SaveFavorites(HashSet<int> favs) =>
    HttpContext.Session.SetString(FavSessionKey, JsonSerializer.Serialize(favs));
```

`ToggleFavorite(int id)` adds or removes the product from the set and returns a JSON response:
```json
{ "isFavorite": true, "productId": 42 }
```

The Highlights page (`/Products/Highlights`) shows products where `DiscountPercent > 0` OR the product is in the favorites set.

---

## Views & UI

### Layout (`_Layout.cshtml`)

- Sticky top navigation bar with brand, page links, cart badge, theme toggle, user menu.
- The antiforgery token is embedded as `<meta name="request-verification-token">` for AJAX use.
- Dark/light theme toggle persists preference in `localStorage`.
- `TempData["SuccessMessage"]` and `TempData["Error"]` are displayed as Bootstrap alerts below the header.

### `_ProductCard.cshtml` (partial)

Renders a grid of product cards. Used both in the full page and as the AJAX-swappable inner content for filtering.

### `_UserMenu.cshtml` (partial)

Shows the current user's name/email and logout button if authenticated, or Login/Register links if not.

### Product Card Buttons

- Regular users see "Adicionar ao carrinho" (add to cart) and "♡" (favorite toggle).
- Admin/staff see "✏️ Editar" and "🗑️ Eliminar" instead of the cart button.

---

## CSS & Theming

### Design System (CSS Custom Properties)

`site.css` defines a full design token system using CSS variables:

```css
:root {
    --brand:           #e87600;   /* primary orange */
    --brand-dark:      #c96700;
    --accent:          #ffb14e;
    --radius:          0.875rem;
    --shadow:          0 4px 16px rgba(0,0,0,0.10);
}

body[data-theme="light"] { --bg: #fdf8f3; --card: #fff; --text: #1a1a2e; ... }
body[data-theme="dark"]  { --bg: #0d1117; --card: #1c2128; --text: #f0f6fc; ... }
```

Theme switching is handled purely via JS toggling `data-theme` on `<body>`.

### Login/Register Styles

The login card uses pill-shaped (`border-radius: 999px`) inputs and button for a modern look. Both inputs and the submit button are `width: 100%` so they align perfectly.

---

## API Project (WEBAPITest)

The Web API uses:
- **EF Core** with SQL Server (`diogoportela_SchoolBarContext`)
- **JWT Bearer** authentication (same key/issuer/audience as the MVC project)
- **Swagger** with JWT security definition
- **CORS** allowing `https://localhost:7223` (MVC port)

Key API controllers:
| Controller | Base Route | Description |
|---|---|---|
| `AuthController` | `api/Auth` | Login (`/login`) and register (`/register`) — returns JWT |
| `ProductsController` | `api/Products` | Public product read access |
| `ProductsAdminController` | `api/ProductsAdmin` | Admin CRUD + image upload |
| `CategoriesController` | `api/Categories` | Category CRUD |
| `OrdersController` | `api/Orders` | Cart management (get cart, add, update, remove) |
| `MenusController` | `api/Menus` | Menu read access with date range filter |
| `LunchBookingsController` | `api/LunchBookings` | Book and cancel lunch; get my bookings |
| `UsersController` | `api/Users` | User profile (`/me`) |

---

## Configuration

**`appsettings.json`** (MVC project):
```json
{
  "Jwt": {
    "Authority": "api-escolar",
    "Audience":  "mvc-client",
    "SigningKey": "<min 32-char secret>"
  }
}
```

The `BaseAddress` for the API client is hardcoded in `Program.cs`:
```csharp
client.BaseAddress = new Uri("https://localhost:7234/");
```
Change this if the API runs on a different port.

---

## Running the Project

Both projects must run simultaneously.

1. **Start the Web API:**
   ```bash
   cd WEBAPITest/WEBAPITest
   dotnet run
   ```
   API will be available at `https://localhost:7234`.

2. **Start the MVC front-end:**
   ```bash
   cd WEBAPITest/MVCAPIFriedBananas
   dotnet run
   ```
   MVC will be available at `https://localhost:7223`.

3. Open `https://localhost:7223` in a browser.

> **Note:** Both projects must use the same JWT `SigningKey`, `Authority`, and `Audience` values in their respective `appsettings.json` files, otherwise token validation will fail.
