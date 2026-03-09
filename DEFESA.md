# Defesa do Projeto — BarEscola
## Perguntas e Respostas para Apresentação

Ordenadas do mais fácil ao mais difícil. O foco é em **linhas de código concretas**, lógica de controllers e serviços.

---

## BLOCO 1 — Estrutura Geral e Fluxo de Dados

---

**P: O projeto tem dois projetos separados. Qual é o papel de cada um?**

R: `WEBAPITest` é a Web API — contém a base de dados (via Entity Framework), os modelos, e os endpoints HTTP que devolvem JSON. `MVCAPIFriedBananas` é a aplicação MVC — é o front-end que o utilizador vê no browser. A MVC nunca fala com a base de dados diretamente; toda a comunicação passa pela API através de classes chamadas ApiClients.

---

**P: Como é que o MVC sabe para onde enviar os pedidos HTTP? Onde está essa configuração?**

R: Em `Program.cs` da MVC, na linha:
```csharp
client.BaseAddress = new Uri("https://localhost:7234/");
```
Esta linha define o endereço base do HttpClient chamado `"BarEscolaApi"`. Todos os ApiClients (ProductsApiClient, BookingApiClient, etc.) pedem essa instância com:
```csharp
_httpClient = httpClientFactory.CreateClient("BarEscolaApi");
```
Por isso, quando `ProductsApiClient` faz `GetFromJsonAsync("api/Products")`, o pedido real vai para `https://localhost:7234/api/Products`.

---

**P: O que é o `builder.Services.AddScoped<ProductsApiClient>()` e porquê está em `Program.cs`?**

R: Regista `ProductsApiClient` no sistema de injeção de dependências (DI) com ciclo de vida `Scoped` — uma instância por pedido HTTP. Quando o `ProductsController` declara no construtor:
```csharp
public ProductsController(ProductsApiClient products, ...)
```
o ASP.NET cria automaticamente uma instância de `ProductsApiClient` e injeta-a. Sem este registo em `Program.cs`, o controller lançaria uma exceção a dizer que não consegue resolver o serviço.

---

**P: O que faz a linha `app.UseSession()` e porquê vem antes de `app.UseAuthentication()`?**

R: `UseSession()` ativa o middleware de sessão HTTP — é ele que carrega os dados da sessão (incluindo o token JWT guardado em `"JwtToken"`) antes de processar o pedido. Tem de vir antes de `UseAuthentication()` porque a autenticação lê o token da sessão. Se a ordem fosse invertida, quando o middleware de autenticação tentasse ler `Session.GetString("JwtToken")`, a sessão ainda não estaria inicializada e devolveria sempre `null`.

---

**P: O que é `nameof(Index)` usado em `return RedirectToAction(nameof(Index))`? Porque não escrever `"Index"` diretamente?**

R: `nameof(Index)` é um operador de C# que devolve o nome do símbolo como string em tempo de compilação — neste caso `"Index"`. A vantagem é que se o nome do método mudar, o compilador apanha o erro imediatamente. Com `"Index"` (string literal), se o método fosse renomeado, o código compilava mas o redirect falhava em runtime sem aviso.

---

## BLOCO 2 — CartController e OrderApiClient

---

**P: No `CartController`, o método `Add` tem esta linha: `if (qty < 1) qty = 1;`. Porquê?**

R: O `qty` vem dos parâmetros do pedido HTTP. Um utilizador malicioso (ou um bug no frontend) poderia enviar `qty=0` ou `qty=-5`. Esta linha garante um mínimo de 1 unidade, evitando que sejam adicionadas quantidades inválidas ao carrinho. É uma validação defensiva no controller antes de chamar a API.

---

**P: No `CartController`, o que faz o método `IsAjax()` e onde é chamado?**

R: `IsAjax()` verifica se o pedido HTTP veio de JavaScript (fetch/XHR) ou de um formulário normal:
```csharp
private bool IsAjax() =>
    HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
    HttpContext.Request.Headers["Accept"].ToString().Contains("application/json", ...);
```
Quando o JS faz `fetch(addCartUrl, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })`, o header `X-Requested-With` é enviado com o valor `"XMLHttpRequest"`. O controller deteta isso e, em vez de redirecionar para a página do carrinho, devolve JSON: `return Json(new { success = true, cartCount = ... })`. O JS recebe esse JSON e atualiza apenas o contador no navbar, sem recarregar a página.

---

**P: No `CartController.Add()`, depois de chamar `AddItemAsync`, há uma segunda chamada a `GetCartAsync()`. Porquê?**

R:
```csharp
await _orders.AddItemAsync(id, qty);
if (IsAjax())
{
    var order = await _orders.GetCartAsync();
    return Json(new { success = true, cartCount = order.Items?.Sum(i => i.Quantity) ?? 0 });
}
```
`AddItemAsync` não devolve o carrinho atualizado — apenas executa a operação. Para saber o total de itens depois da adição, é necessário ir buscar o carrinho à API novamente. O `Sum(i => i.Quantity)` soma todas as quantidades de todos os itens, para mostrar o total correto no badge do navbar.

---

**P: O que faz `ToVm()` no `CartController` e porquê existe este método?**

R: `ToVm()` (To ViewModel) converte o `OrderDto` — que vem da API — para o `OrderViewModel` que a view Razor espera. São tipos diferentes: o `OrderDto` é o contrato entre MVC e API (definido em `DTO_MVCAPIContracts`), enquanto o `OrderViewModel` pertence à camada MVC e tem, por exemplo, a propriedade calculada `ItemCount`:
```csharp
public int ItemCount => Items.Sum(i => i.Quantity);
```
Esta separação evita que a view dependa diretamente da estrutura da API.

---

**P: No `OrderApiClient` (`CartApiController.cs`), porquê é que `GetCartAsync()` verifica o token antes de chamar a API?**

R:
```csharp
if (_ctx.HttpContext?.Session.GetString("JwtToken") == null)
    return new OrderDto { Items = Array.Empty<OrderItemDto>(), Subtotal = 0m, Total = 0m };
```
Se o utilizador não está autenticado, não tem token na sessão. Chamar a API sem token resultaria num 401 Unauthorized. Em vez de lançar uma exceção (que causaria um erro na página), devolve um carrinho vazio imediatamente. Isso permite que a navbar mostre "0" no carrinho para utilizadores não autenticados sem erros visíveis.

---

**P: No `OrderApiClient`, as rotas estão definidas como constantes. Qual é a vantagem?**

R:
```csharp
private const string OrderGetRoute    = "api/Orders/cart";
private const string OrderAddRoute    = "api/Orders/add";
private const string OrderUpdateRoute = "api/Orders/update";
private const string OrderRemoveRoute = "api/Orders/remove";
```
Se a rota da API mudar (e.g., de `api/Orders/add` para `api/Cart/add`), só é preciso alterar num lugar — a constante. Sem isto, a string `"api/Orders/add"` estaria repetida em vários métodos, e era fácil esquecer de atualizar uma delas.

---

**P: No `OrderApiClient`, o método `Auth()` adiciona o token ao header. Porque é que o `ApiAuthHandler` não é suficiente sozinho?**

R: O `ApiAuthHandler` é um `DelegatingHandler` que interceta todos os pedidos do HttpClient nomeado `"BarEscolaApi"`. No entanto, o `OrderApiClient` não está na mesma instância nomeada que os outros — é registado com `AddScoped` e recebe o HttpClient do `IHttpClientFactory`. O `Auth()` manual garante que o header é sempre definido, independentemente da ordem de execução dos middlewares. É uma camada extra de segurança.

---

**P: No `OrdersService` (API), o que faz o método `Recalc()` e quando é chamado?**

R:
```csharp
private void Recalc(Orders order)
{
    order.Items ??= new List<OrderItem>();
    order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);
    order.UpdatedAt = DateTime.UtcNow;
}
```
Sempre que o carrinho é modificado (item adicionado, atualizado ou removido), `Recalc()` recalcula o `Total` somando `UnitPrice * Quantity` de cada item. É chamado antes de `SaveChangesAsync()` para garantir que o total guardado na base de dados está sempre correto. O `??=` garante que `Items` não é null antes do `Sum`.

---

**P: No `OrdersService.AddItemAsync()`, o que acontece se o produto já estiver no carrinho?**

R:
```csharp
var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
if (item == null)
{
    cart.Items.Add(new OrderItem { ProductId = product.ProdId, Quantity = qty, UnitPrice = product.Price ?? 0m });
}
else
{
    item.Quantity += qty;
}
```
Se `FirstOrDefault` devolver `null`, o produto ainda não está no carrinho e é criado um novo `OrderItem`. Se já existir, apenas incrementa a quantidade existente. Assim não há duplicados — adicionar o mesmo produto duas vezes não cria duas linhas, apenas aumenta a quantidade.

---

**P: No `CreateOrderAsync()`, porquê é usado `BeginTransactionAsync()`?**

R:
```csharp
using var tx = await _db.Database.BeginTransactionAsync();
try { ... await tx.CommitAsync(); }
catch { await tx.RollbackAsync(); ... }
```
Uma transação garante atomicidade: ou todas as operações (criar a encomenda, decrementar o stock de cada produto) são gravadas juntas, ou nenhuma é. Se, por exemplo, o segundo produto não tiver stock suficiente, o `RollbackAsync()` desfaz tudo o que foi feito até aí — incluindo o decremento do stock do primeiro produto. Sem transação, poderia ficar-se com stock decrementado mas sem encomenda criada.

---

## BLOCO 3 — ProductsController e Filtros

---

**P: No `ProductsController.Index()`, porquê é que os produtos são filtrados com `.Where(p => p.ProductType == 0)` antes de qualquer outra coisa?**

R: `ProductType == 0` identifica os produtos do bar (snacks, bebidas, etc.). Os menus de cantina têm `ProductType` diferente de 0. A listagem pública de produtos (`/Products`) só deve mostrar produtos de bar — os menus aparecem na página `/Menu`. O `AdminIndex` não aplica este filtro porque o admin precisa de ver todos os produtos.

---

**P: Como funciona o método `ToCard()` e porquê existe separado do construtor do ViewModel?**

R:
```csharp
private static ProductsViewModel ToCard(Product p, List<Category> categories)
{
    var categoryName = categories.FirstOrDefault(c => c.CatId == p.CatId)?.Name ?? $"Categoria {p.CatId}";
    return new ProductsViewModel { ... CategoryName = categoryName };
}
```
É um método estático de mapeamento que converte o modelo `Product` (que tem `CatId` numérico) para o `ProductsViewModel` (que tem `CategoryName` como texto legível). A lista de categorias é passada como parâmetro para evitar ir à API para cada produto individualmente. O `?.Name` é o operador null-conditional — se não encontrar a categoria, usa `"Categoria {CatId}"` como fallback.

---

**P: No `ApplyFilters()`, porque é que os filtros são aplicados em sequência e não todos de uma vez?**

R: Cada filtro é aplicado ao resultado do anterior. Se o utilizador selecionar categoria "Bebidas" e pesquisa "laranja", primeiro filtra só os produtos da categoria, depois filtra esses pelo texto. A sequência importa para a eficiência: se a categoria já reduziu de 100 para 10 produtos, a pesquisa seguinte corre em 10 items. A variável `list` vai sendo substituída:
```csharp
var list = products;
if (q.FavoritesOnly) list = list.Where(p => p.IsFavorite == true);
if (!string.IsNullOrWhiteSpace(q.SelectedCategory)) list = list.Where(p => ...).ToList();
```
O `.ToList()` no final de cada filtro materializa a query em memória antes do próximo filtro.

---

**P: No `ApplyFilters()`, o filtro de preços usa um switch expression. Como funciona a linha `_ => list`?**

R:
```csharp
list = q.PriceRange switch
{
    "lt5"   => list.Where(p => (p.Price ?? 0m) < 5m),
    "5to10" => list.Where(p => (p.Price ?? 0m) >= 5m && (p.Price ?? 0m) <= 10m),
    "gt10"  => list.Where(p => (p.Price ?? 0m) > 10m),
    _       => list
};
```
O `_` é o padrão wildcard (default) do switch expression — equivale ao `default:` num switch statement. Se `PriceRange` não for nenhum dos valores conhecidos (e.g., string vazia ou valor inesperado), devolve `list` sem alterar. O `?? 0m` trata produtos sem preço definido como se custassem 0 euros.

---

**P: No `ProductsController`, o que faz `SplitAllergens()` e porquê é necessário?**

R:
```csharp
private static IEnumerable<string> SplitAllergens(string allergens) =>
    allergens.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
             .Select(a => a.Trim())
             .Where(a => !string.IsNullOrWhiteSpace(a));
```
Os alergénios são guardados na base de dados como uma string separada por vírgulas ou ponto-e-vírgula: `"gluten,lactose,soja"`. Para filtrar por um alergénio específico, é necessário separar a string em itens individuais. O `Split` divide, o `Select(Trim)` remove espaços extra, e o `Where` elimina entradas vazias que poderiam resultar de vírgulas duplas (`"gluten,,lactose"`).

---

**P: No `ProductsController`, o que faz `ToggleFavorite()` e porque devolve `Json()`?**

R:
```csharp
public IActionResult ToggleFavorite(int id)
{
    var favs = GetFavorites();
    bool isFav;
    if (favs.Contains(id)) { favs.Remove(id); isFav = false; }
    else { favs.Add(id); isFav = true; }
    SaveFavorites(favs);
    return Json(new { isFavorite = isFav, productId = id });
}
```
Lê o `HashSet<int>` de favoritos da sessão. Se o produto já está no set, remove (desfavoritar); caso contrário, adiciona (favoritar). Guarda o set atualizado na sessão. Devolve JSON porque é chamado via AJAX pelo JavaScript da página — o JS recebe `{ isFavorite: true/false }` e atualiza o ícone do botão (♥/♡) sem recarregar a página.

---

**P: O que são `GetFavorites()` e `SaveFavorites()` e como guardam dados entre pedidos?**

R:
```csharp
private HashSet<int> GetFavorites()
{
    var json = HttpContext.Session.GetString(FavSessionKey);
    if (string.IsNullOrEmpty(json)) return new HashSet<int>();
    return JsonSerializer.Deserialize<HashSet<int>>(json) ?? new HashSet<int>();
}

private void SaveFavorites(HashSet<int> favs) =>
    HttpContext.Session.SetString(FavSessionKey, JsonSerializer.Serialize(favs));
```
A sessão HTTP só armazena strings (ou bytes). Por isso os favoritos são convertidos para JSON (`"[1,5,12]"`) antes de guardar. Para ler, faz-se o processo inverso: pega na string da sessão e desserializa para `HashSet<int>`. O `HashSet` impede duplicados automaticamente — se chamar `.Add(5)` duas vezes, o 5 só aparece uma vez.

---

**P: A página `/Products/Highlights` mostra produtos em destaque. Qual é a condição exata usada?**

R:
```csharp
var highlighted = products
    .Where(p => (p.DiscountPercent > 0) || favIds.Contains(p.ProdId))
    .ToList();
```
Um produto aparece em Highlights se tiver desconto (`DiscountPercent > 0`) **ou** se o utilizador o tiver marcado como favorito (`favIds.Contains(p.ProdId)`). Os `favIds` são lidos da sessão antes desta linha.

---

## BLOCO 4 — MenuController e BookingController

---

**P: No `MenuController.Index()`, como é calculada a segunda-feira da semana atual?**

R:
```csharp
var today = DateOnly.FromDateTime(DateTime.Today);
var start = weekStart ?? today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
```
`DayOfWeek` é um enum onde `Sunday=0, Monday=1, Tuesday=2, ..., Saturday=6`. Se hoje for quarta-feira (3), o cálculo é `-(3) + 1 = -2`, então `AddDays(-2)` volta para segunda. Se for segunda (1): `-(1) + 1 = 0`, fica na própria segunda. Se for domingo (0): `-(0) + 1 = +1`, avança para segunda.

O `weekStart ?? today.AddDays(...)` usa o operador null-coalescing: se o parâmetro `weekStart` foi passado na URL, usa esse; caso contrário calcula o Monday da semana atual.

---

**P: Por que existe a verificação de sábado e domingo depois de calcular o `start`?**

R:
```csharp
if (start.DayOfWeek == DayOfWeek.Saturday) start = start.AddDays(2);
else if (start.DayOfWeek == DayOfWeek.Sunday)  start = start.AddDays(1);
```
Quando o utilizador navega para a semana seguinte a partir de uma sexta-feira, pode acontecer que o `weekStart` enviado na URL seja um sábado ou domingo. Nesse caso, a visualização avança para a segunda-feira seguinte. Sem esta verificação, o grid mostraria um sábado como primeiro dia.

---

**P: Como é gerada a lista de 5 dias no `WeeklyMenuViewModel`?**

R:
```csharp
Days = Enumerable.Range(0, 5).Select(i => BuildDay(start.AddDays(i), dtoList)).ToList()
```
`Enumerable.Range(0, 5)` gera os inteiros 0, 1, 2, 3, 4. Para cada `i`, `start.AddDays(i)` calcula segunda (0), terça (1), quarta (2), quinta (3), sexta (4). `BuildDay()` recebe cada data e filtra os menus desse dia a partir da `dtoList`.

---

**P: O que faz `BuildDay()` no `MenuController`?**

R:
```csharp
private static MenuDayViewModel BuildDay(DateOnly day, IEnumerable<MenusDto> dtoList)
{
    var daily = dtoList
        .Where(m => m.Date.HasValue && DateOnly.FromDateTime(m.Date.Value) == day)
        .ToList();

    return new MenuDayViewModel
    {
        Date       = day,
        Normal     = MapItem(daily.FirstOrDefault(m => m.Type == false)),
        Vegetarian = MapItem(daily.FirstOrDefault(m => m.Type == true))
    };
}
```
Filtra os menus da `dtoList` que correspondem ao `day` específico. Depois procura o menu normal (`Type == false`) e o vegetariano (`Type == true`). O `FirstOrDefault` devolve `null` se não houver menu desse tipo nesse dia — daí `MapItem()` aceitar `MenusDto?` e devolver `null` nesses casos (a view trata o `null` mostrando "Sem menu disponível").

---

**P: No `MenuController.Index()`, como é que a view sabe que dias já têm reserva?**

R:
```csharp
var bookedDates = new HashSet<DateOnly>();
if (User.Identity?.IsAuthenticated == true)
{
    var myBookings = await _bookings.GetMyBookingsAsync();
    foreach (var b in myBookings)
    {
        var d = DateOnly.FromDateTime(b.Date);
        if (d >= start && d <= end)
            bookedDates.Add(d);
    }
}
```
Vai buscar todas as reservas do utilizador à API. Para cada reserva, converte a data para `DateOnly` e verifica se está dentro da semana a mostrar (`d >= start && d <= end`). As datas que passam são adicionadas ao `HashSet<DateOnly>`. Na view, para cada dia, verifica-se `Model.BookedDates.Contains(day.Date)` — se `true`, o cartão fica verde e o botão de reserva fica desativado.

---

**P: No `BookingController.Book()`, o que é `var (success, message) = await _bookings.BookAsync(menuId)`?**

R: É desestruturação de tuplo. O método `BookAsync()` em `BookingApiClient` devolve `(bool Success, string Message)`:
```csharp
public async Task<(bool Success, string Message)> BookAsync(int menuId)
```
Em vez de declarar uma variável separada para o resultado e aceder a `.Success` e `.Message`, a desestruturação extrai os dois valores diretamente para `success` e `message` numa só linha.

---

**P: No `BookingController`, o que é `TempData` e porque é usado para mensagens?**

R: `TempData` é um dicionário especial do MVC que guarda dados para o **próximo pedido HTTP e só esse**. Após `Book()` ou `Cancel()`, o controller faz um redirect (`return RedirectToAction(...)`). Num redirect, o pedido HTTP atual termina e começa um novo pedido. Se usasse `ViewData`, os dados perder-se-iam no redirect. `TempData` persiste através do redirect (internamente usa a sessão) e é limpo automaticamente depois de ser lido. Assim a mensagem "Reserva efetuada com sucesso! ✅" aparece na página seguinte depois do redirect.

---

**P: No `BookingController`, o `Cancel()` tem esta verificação: `if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))`. Porquê o `IsLocalUrl`?**

R: `IsLocalUrl()` verifica que o URL é do próprio site (começa com `/`) e não de um domínio externo. Sem esta verificação, um atacante poderia construir um link como `/Booking/Cancel?bookingId=5&returnUrl=https://site-malicioso.com` e redirecionar o utilizador para fora do site após cancelar. Esta validação é uma proteção contra "Open Redirect" — não tem a ver com o JWT.

---

**P: No `LunchBookingService` (API), quais são as regras de negócio do `BookLunchAsync()`?**

R: Há quatro verificações em sequência:
1. **Menu existe?** — `if (menu is null) return OperationResult.Fail("Menu não encontrado.")`
2. **Hora do dia:** `if (menuDate == now.Date && now.TimeOfDay > TimeSpan.FromHours(10))` — não se pode reservar para o próprio dia depois das 10h.
3. **Já tem reserva nesse dia?** — `_db.LunchBookings.AnyAsync(lb => lb.UId == userId && lb.MIdNavigation.Date.Value.Date == menuDate)` — um utilizador não pode ter mais do que uma reserva por dia.
4. **Vagas disponíveis?** — `if (menu.AvailableSeats <= 0)` — o menu tem um limite de lugares.
Só depois de todas passarem é que a reserva é criada.

---

**P: No `LunchBookingService`, o que acontece aos contadores depois de uma reserva?**

R:
```csharp
menu.UsedSeats = (menu.UsedSeats ?? 0) + 1;
menu.AvailableSeats = menu.AvailableSeats - 1;
_db.LunchBookings.Add(booking);
await _db.SaveChangesAsync();
```
O menu tem três campos relacionados: `MaxSeats`, `UsedSeats` e `AvailableSeats`. Ao reservar, `UsedSeats` aumenta 1 e `AvailableSeats` diminui 1. Ao cancelar (`CancelBookingAsync`), o inverso acontece. Assim `AvailableSeats` reflete em tempo real as vagas que sobram para esse menu.

---

**P: No `BookingApiClient.BookAsync()`, o corpo do POST é `null`. Porquê?**

R:
```csharp
var response = await _httpClient.PostAsync($"api/LunchBookings/book/{menuId}", null);
```
O `menuId` vai no URL da rota (`/book/{menuId}`), não no corpo do pedido. A API extrai-o do URL com o parâmetro `[HttpPost("book/{menuId}")]`. Não há dados adicionais a enviar no body, portanto passa-se `null` como segundo argumento de `PostAsync`.

---

## BLOCO 5 — AuthController

---

**P: No `AuthController.Login()`, porquê é que o token é extraído com `JsonDocument.Parse()` em vez de desserializar diretamente para uma classe?**

R:
```csharp
using var doc = JsonDocument.Parse(content);
var root = doc.RootElement;
if (root.TryGetProperty("token", out var p) || root.TryGetProperty("Token", out p))
    token = p.GetString();
```
A API pode devolver `{ "token": "..." }` (camelCase) ou `{ "Token": "..." }` (PascalCase) dependendo da configuração. `TryGetProperty("token", out var p) || TryGetProperty("Token", out p)` tenta as duas formas — se a primeira falhar, tenta a segunda. `JsonDocument` é mais flexível do que desserializar para uma classe com um nome de propriedade fixo. O `using` garante que os recursos do `JsonDocument` são libertados depois de usar.

---

**P: Depois do login bem-sucedido, o que acontece exatamente? Indica as linhas concretas.**

R:
```csharp
HttpContext.Session.SetString("JwtToken", token);       // guarda na sessão
TempData["SuccessMessage"] = "Login efetuado com sucesso!"; // mensagem para próxima página
return RedirectToAction("Index", "Home");               // redireciona
```
O token JWT é guardado na sessão do servidor com a chave `"JwtToken"`. Uma mensagem de sucesso é posta no `TempData` para ser exibida na página seguinte. Por fim, redireciona para `HomeController.Index()`.

---

**P: No `AuthController.Logout()`, o que faz `HttpContext.Session.Remove("JwtToken")`?**

R: Remove a entrada `"JwtToken"` do dicionário da sessão. Como o `OnMessageReceived` do JWT ****** lê o token de `Session.GetString("JwtToken")`, assim que essa chave é removida, os pedidos seguintes desse utilizador não terão token e serão tratados como anónimos. É o equivalente a "desautenticar" o utilizador sem invalidar o JWT na API (o que exigiria uma blacklist de tokens no servidor).

---

## BLOCO 6 — CategoriesController

---

**P: No `CategoriesController.Edit()`, porquê há `if (id != model.CatId) return BadRequest()`?**

R: O `id` vem da URL (rota `/Categories/Edit/5`) e o `model.CatId` vem do formulário HTML (campo hidden). Se forem diferentes, alguém pode estar a tentar fazer PUT num ID diferente do que está no body — possível tentativa de manipulação. `BadRequest()` devolve HTTP 400 antes de qualquer modificação na base de dados.

---

**P: No `CategoriesController`, o `Delete` tem dois métodos com o mesmo nome. Como é que funciona?**

R:
```csharp
[Authorize]
public async Task<IActionResult> Delete(int id) { ... }   // GET

[HttpPost, ActionName("Delete")]
[Authorize]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteConfirmed(int id) { ... }   // POST
```
O primeiro é o GET — mostra a página de confirmação de eliminação. O segundo é o POST — executa a eliminação. `[ActionName("Delete")]` diz ao MVC para tratar `DeleteConfirmed` como se se chamasse `Delete` para efeitos de routing, mas como tem `[HttpPost]`, só é chamado em pedidos POST. Isto evita conflito de nomes em C# enquanto o MVC consegue rotear corretamente.

---

**P: Na API `CategoriesController`, o `DeleteCATEGORY()` verifica produtos associados antes de eliminar. Porquê?**

R:
```csharp
if (category.Products != null && category.Products.Any())
    return BadRequest("Não é possível eliminar a categoria: existem produtos associados.");
```
Sem esta verificação, eliminar uma categoria deixaria os produtos com uma `CatId` que já não existe na tabela `Categories` — uma "foreign key" inválida que quebraria as relações na base de dados. O EF Core em alguns casos lançaria uma exceção de integridade referencial. Esta verificação explícita devolve uma mensagem clara ao utilizador em vez de um erro genérico.

---

## BLOCO 7 — AJAX, JavaScript e Views

---

**P: No JavaScript de `Products/Index.cshtml`, o que faz exatamente a função `postCart(productId)`?**

R:
```javascript
async function postCart(productId) {
    if (!isLoggedIn) { window.location.href = '/Auth/Login'; return; }
    const params = new URLSearchParams();
    params.append('id', productId);
    params.append('qty', 1);
    if (token) params.append('__RequestVerificationToken', token);
    const res = await fetch(addCartUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'X-Requested-With': 'XMLHttpRequest' },
        body: params.toString()
    });
    const json = await res.json().catch(() => ({}));
    const badge = document.querySelector('[data-cart-count]');
    if (badge && json?.cartCount !== undefined) badge.textContent = json.cartCount;
}
```
1. Se não está autenticado, redireciona para login sem fazer o pedido.
2. Constrói os parâmetros do formulário em `URLSearchParams`, incluindo o token antiforgery.
3. Faz um `fetch` POST com o header `X-Requested-With: XMLHttpRequest` (para o `IsAjax()` do controller detetar que é AJAX).
4. Recebe o JSON `{ success, cartCount }` e atualiza o badge no navbar com o novo total.

---

**P: No JavaScript, o que faz `wireCartButtons(root = document)` e porquê é chamado de novo depois de `fetchProducts()`?**

R:
```javascript
function wireCartButtons(root = document) {
    root.querySelectorAll('[data-add-cart]').forEach(btn => {
        btn.onclick = async () => { ... await postCart(id); ... };
    });
}
```
Encontra todos os botões com `data-add-cart` dentro de `root` e atribui o handler de clique. Quando `fetchProducts()` é chamada (filtro AJAX), o HTML dos cards é substituído com `container.innerHTML = html` — os novos botões são elementos novos no DOM e não têm handlers. Por isso `wireCartButtons(container)` é chamado depois do filtro, passando apenas o `container` para só re-ligar os botões novos.

---

**P: No `_ProductCard.cshtml`, o que é `data-add-cart` no botão e para que serve?**

R:
```html
<button type="button" class="btn btn-primary w-100"
        data-add-cart
        data-product-id="@p.ProdId">
    Adicionar
</button>
```
`data-add-cart` é um atributo HTML personalizado (data attribute) sem valor. O JavaScript usa-o como seletor: `querySelectorAll('[data-add-cart]')`. `data-product-id="@p.ProdId"` passa o ID do produto ao JS de forma segura através do DOM: `btn.getAttribute('data-product-id')`. Isto evita ter IDs hardcoded no JavaScript e funciona para todos os produtos dinamicamente.

---

**P: O que é `@await Html.PartialAsync("_ProductCard", Model.Items)` na view `Index.cshtml`?**

R: Renderiza a partial view `_ProductCard.cshtml` passando `Model.Items` como modelo. O `PartialAsync` é assíncrono para não bloquear a thread. Na primeira carga da página, gera o HTML dos cards no servidor. O mesmo HTML é também o que `GET /Products/Filter` devolve quando o JS faz o pedido AJAX de filtro — a partial view é reutilizada, garantindo consistência entre a carga inicial e o filtro.

---

**P: A função `fetchProducts()` em JS chama `GET /Products/Filter`. O que acontece no controller?**

R:
```csharp
[HttpGet]
public async Task<IActionResult> Filter([FromQuery] ProductsPageViewModel query)
{
    var products = (await _products.GetProductsAsync()).Where(p => p.ProductType == 0).ToList();
    ApplyFavoritesToProducts(products);
    var categoriesList = await _categories.GetCategoriesAsync();
    var filtered = ApplyFilters(products, query).Select(p => ToCard(p, categoriesList)).ToList();
    return PartialView("_ProductCard", filtered);
}
```
Recebe os filtros como query string (`?Search=sandes&SelectedCategory=Bebidas`), aplica-os, e devolve a partial view `_ProductCard` com os produtos filtrados. Devolver uma `PartialView` em vez de `View` retorna apenas o HTML do fragmento, sem o `_Layout`. É esse HTML que o JS insere no `container.innerHTML`.

---

## BLOCO 8 — Modelos, ViewModels e OperationResult

---

**P: Qual é a diferença entre `Product` (em `MVCAPIFriedBananas/Models`) e `Products` (em `WEBAPITest/Models`)?**

R: `Products` (API) é a entidade Entity Framework — está diretamente mapeada para a tabela `Products` da base de dados e tem propriedades `nullable` como `public int? CurStock { get; set; }`. `Product` (MVC) é o modelo do lado do cliente — tem as mesmas colunas mas pode ter propriedades extras como `IsFavorite` que não existem na BD (são calculadas em memória na sessão). São tipos separados para que mudanças na BD não quebrem automaticamente o front-end.

---

**P: No `ProductsPageViewModel`, o que é `CategoryList` e porquê é diferente de `Categories`?**

R:
```csharp
public IEnumerable<string> Categories { get; set; }    // nomes para o dropdown do filtro
public List<Category> CategoryList { get; set; } = new(); // objetos completos para o AdminIndex
```
`Categories` é uma lista de strings com apenas os nomes — é o que o filtro público precisa para mostrar no dropdown. `CategoryList` tem os objetos `Category` completos (com `CatId`, `Name`, `ImgPath`) — é necessário no `AdminIndex` para o formulário de edição de produtos, onde é preciso o `CatId` para o `<select>`. No `Index` público, `CategoryList` fica vazio para não desperdiçar memória.

---

**P: O que é `OperationResult<T>` e como é usado no `OrdersService`?**

R:
```csharp
public sealed record OperationResult<T>(bool Success, string Message, T? Data)
{
    public static OperationResult<T> Ok(T data, string message = "OK") => new(true, message, data);
    public static OperationResult<T> Fail(string message) => new(false, message, default);
}
```
É um wrapper que evita lançar exceções para situações de negócio esperadas. Em vez de `throw new Exception("Produto não encontrado")`, o serviço devolve `OperationResult<OrderDto>.Fail("Produto não encontrado")`. O controller verifica `if (!res.Success) return BadRequest(new { message = res.Message })`. É mais limpo e previsível do que capturar exceções para fluxo de controlo normal.

---

**P: No `OrderViewModel`, o que é `public int ItemCount => Items.Sum(i => i.Quantity)` e como funciona?**

R: É uma propriedade calculada (get-only, sem `set`). Em vez de ser um campo guardado, calcula o valor sempre que é acedido. `Items.Sum(i => i.Quantity)` usa LINQ para somar todas as quantidades de todos os items. Por exemplo, se o carrinho tiver 2 batatas fritas e 3 sumos, `ItemCount` devolve 5. A view pode usar `@Model.ItemCount` para mostrar o total de itens sem precisar de fazer o cálculo no Razor.

---

**P: No `WeeklyMenuViewModel`, `BookedDates` é um `HashSet<DateOnly>` e não uma `List<DateOnly>`. Porquê?**

R: `HashSet<T>` usa uma tabela hash internamente, tornando `Contains()` uma operação O(1) — tempo constante independentemente do número de reservas. A view verifica `Model.BookedDates.Contains(day.Date)` para **cada um dos 5 dias** do grid. Com `List`, seria O(n) por cada verificação. Como as reservas de um utilizador podem acumular ao longo do ano, o `HashSet` é mais eficiente. Além disso, impede datas duplicadas, embora na prática isso não aconteça (um utilizador não tem duas reservas no mesmo dia).

---

## BLOCO 9 — Validações e Erros

---

**P: No `AuthController.Login()`, porquê há um `try/catch` a envolver toda a lógica?**

R:
```csharp
try
{
    var response = await _httpClient.PostAsJsonAsync("api/Auth/login", model);
    ...
}
catch (Exception ex)
{
    ModelState.AddModelError(string.Empty, $"Erro inesperado: {ex.Message}");
    return View(model);
}
```
Se a API estiver em baixo, se houver timeout de rede, ou qualquer outro erro não previsto, a exceção é capturada e convertida numa mensagem de erro para o utilizador. Sem o `try/catch`, uma exceção não tratada causaria uma página de erro 500. Com ele, o utilizador vê a mensagem de erro no próprio formulário de login e pode tentar novamente.

---

**P: O que faz `ModelState.AddModelError(string.Empty, "...")` e porque usa `string.Empty` como chave?**

R: `ModelState.AddModelError(campo, mensagem)` adiciona um erro de validação associado a um campo específico. Quando `campo` é `string.Empty` (ou `""`), o erro não está associado a nenhum campo em particular — é um erro global do formulário. Na view, `@Html.ValidationSummary()` ou a verificação `!ViewData.ModelState.IsValid` mostra estes erros globais. Usar o nome de um campo específico (e.g., `"Email"`) mostraria o erro apenas junto ao campo de email.

---

**P: No `ProductsController.Create()` e `Edit()`, porquê é que `ViewBag.Categories` é carregado tanto no GET como no POST?**

R: No GET, carrega-se as categorias para mostrar o `<select>` populado. Se o POST falhar validação (`!ModelState.IsValid`), a view é devolvida de novo com o modelo e os erros. A view volta a precisar do `ViewBag.Categories` para renderizar o `<select>`. Se não fosse recarregado no POST com erro, o `<select>` ficaria vazio na segunda renderização. `ViewBag` não persiste entre pedidos (ao contrário de `TempData`), por isso tem de ser sempre reatribuído.

---

**P: No `BookingController.ParseMessage()`, o que acontece se a API devolver JSON ou texto simples?**

R:
```csharp
private static string ParseMessage(string raw, string fallback)
{
    if (string.IsNullOrWhiteSpace(raw)) return fallback;
    try
    {
        using var doc = System.Text.Json.JsonDocument.Parse(raw);
        if (doc.RootElement.TryGetProperty("message", out var prop))
            return prop.GetString() ?? fallback;
    }
    catch (System.Text.Json.JsonException) { }
    return raw.Trim('"', ' ');
}
```
A API pode devolver `{"message":"Já existe uma marcação"}` (JSON) ou simplesmente `"Marcação cancelada"` (string). Tenta fazer parse de JSON primeiro — se conseguir e tiver a propriedade `"message"`, usa esse texto. Se o `JsonDocument.Parse()` lançar `JsonException` (não é JSON válido), o `catch` ignora e devolve o `raw` diretamente, removendo aspas externas com `.Trim('"', ' ')`.
