# Defesa do Projeto — BarEscola

Perguntas possíveis de uma apresentação/defesa, ordenadas do mais fácil ao mais difícil. Cada pergunta tem a resposta correta.

---

## Nível 1 — Introdução e Estrutura

**P: O que é este projeto e qual é o seu objetivo?**
R: É uma plataforma web para o bar/cantina de uma escola. Permite aos alunos ver produtos, adicionar ao carrinho, ver o menu semanal e reservar o almoço. Os administradores podem gerir produtos, categorias e menus. É composto por dois projetos: uma Web API (back-end com base de dados) e uma aplicação MVC (front-end).

**P: Qual é a diferença entre uma Web API e uma aplicação MVC?**
R: A Web API expõe endpoints HTTP que devolvem JSON — não tem interface visual. A aplicação MVC gera páginas HTML com Razor e é o que o utilizador vê no browser. A MVC consome a API para obter e guardar dados.

**P: Porquê dividir em dois projetos em vez de fazer tudo num só?**
R: A separação permite que a API possa ser usada por outras aplicações no futuro (uma app mobile, por exemplo). Também facilita a manutenção: o front-end e o back-end evoluem de forma independente, e podem correr em servidores diferentes.

**P: Quais tecnologias/frameworks foram usadas?**
R: ASP.NET Core MVC e Web API (C#, .NET 8), Entity Framework Core com SQL Server, JWT para autenticação, Bootstrap 5 para CSS base, Razor para templates HTML, e session do ASP.NET para persistência do token e favoritos.

**P: O que é o padrão MVC?**
R: MVC significa Model-View-Controller. O Model contém os dados e lógica de negócio. A View apresenta os dados ao utilizador. O Controller recebe os pedidos do utilizador, chama o Model para processar, e devolve uma View com os resultados.

---

## Nível 2 — Autenticação e JWT

**P: O que é um JWT (JSON Web Token)?**
R: É um token de autenticação com três partes separadas por pontos: header (algoritmo), payload (dados/claims) e assinatura. O servidor assina o token com uma chave secreta. O cliente envia o token em cada pedido para provar a sua identidade, e o servidor verifica a assinatura sem precisar de consultar a base de dados.

**P: Como é que o login funciona neste projeto, passo a passo?**
R:
1. O utilizador submete email e password no formulário em `/Auth/Login`.
2. O `AuthController.Login()` faz um POST para `api/Auth/login` na Web API com esses dados.
3. A API verifica as credenciais, gera um JWT e devolve `{ "token": "..." }`.
4. O controller MVC extrai o token do JSON usando `JsonDocument.Parse()` e `TryGetProperty("token", ...)`.
5. Guarda o token na sessão do servidor: `HttpContext.Session.SetString("JwtToken", token)`.
6. Redireciona o utilizador para a página inicial.

**P: Porquê guardar o JWT na sessão do servidor em vez de num cookie diretamente?**
R: Guardar na sessão do servidor (em memória) significa que o token nunca sai para o browser diretamente num cookie com o valor real do JWT. A sessão usa um cookie de ID (`ASP.NET_SessionId`) que não contém o token — é apenas uma chave para o dicionário em memória no servidor. Isto reduz o risco de o token ser interceptado.

**P: Como é que o middleware de autenticação JWT sabe onde está o token se ele está na sessão e não num header HTTP?**
R: O `Program.cs` regista um evento `OnMessageReceived` no JWT Bearer middleware:
```csharp
OnMessageReceived = ctx =>
{
    var token = ctx.HttpContext.Session.GetString("JwtToken");
    if (!string.IsNullOrEmpty(token))
        ctx.Token = token;
    return Task.CompletedTask;
}
```
Este evento é invocado antes de o middleware tentar validar o token. Ao fazer `ctx.Token = token`, estamos a injetar o token no pipeline de validação como se tivesse vindo no header `Authorization: Bearer`.

**P: O que acontece quando um utilizador não autenticado tenta aceder a uma página com `[Authorize]`?**
R: O evento `OnChallenge` do JWT Bearer middleware deteta que não é um pedido AJAX e chama `ctx.HandleResponse()` para suprimir a resposta 401. Depois redireciona para `/Auth/Login?returnUrl=...`:
```csharp
ctx.HandleResponse();
ctx.Response.Redirect($"/Auth/Login?returnUrl={returnUrl}");
```
Para pedidos AJAX, deixa passar o 401 para que o JavaScript o possa tratar.

**P: O que são claims num JWT e como são usados neste projeto?**
R: Claims são pares chave-valor dentro do payload do JWT, como `{ "role": "0", "email": "admin@bar.pt" }`. Neste projeto, o role do utilizador é um claim. Nas views Razor usa-se `User.FindFirst(ClaimTypes.Role)?.Value` para verificar se o utilizador é admin (role `0` ou `1`) e mostrar ou esconder botões de edição.

---

## Nível 3 — HTTP, ApiClients e DI

**P: O que é um `HttpClient` e como é que está configurado neste projeto?**
R: `HttpClient` é a classe do .NET para fazer pedidos HTTP. Neste projeto, é criado um cliente nomeado `"BarEscolaApi"` com `AddHttpClient()`:
```csharp
builder.Services.AddHttpClient("BarEscolaApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:7234/");
})...AddHttpMessageHandler<ApiAuthHandler>();
```
O `BaseAddress` define o URL base da API. O `ApiAuthHandler` é um delegating handler que interceta cada pedido e adiciona o header `Authorization: Bearer <token>` automaticamente.

**P: O que é um `DelegatingHandler` e qual é o do `ApiAuthHandler`?**
R: Um `DelegatingHandler` é um middleware para o pipeline do `HttpClient`. Implementa `SendAsync()`, onde pode modificar o pedido antes de o enviar ou a resposta depois de receber. O `ApiAuthHandler` lê o token JWT da sessão e adiciona-o ao header do pedido:
```csharp
protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
{
    var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
    if (!string.IsNullOrEmpty(token))
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    return base.SendAsync(request, ct);
}
```

**P: O que é Injeção de Dependências (DI) e como é usada neste projeto?**
R: DI é um padrão em que os objetos não criam as suas próprias dependências — recebem-nas do exterior. No `Program.cs`, registamos as dependências:
```csharp
builder.Services.AddScoped<ProductsApiClient>();
builder.Services.AddScoped<CategoriesApiClient>();
```
Depois, o controller recebe-as automaticamente pelo construtor:
```csharp
public ProductsController(ProductsApiClient products, OrderApiClient orders, CategoriesApiClient categories)
```
O ASP.NET Core cria as instâncias corretas e injeta-as. `AddScoped` significa que uma instância é criada por pedido HTTP.

**P: Porquê usar `AddScoped` em vez de `AddSingleton` para os ApiClients?**
R: Os ApiClients acedem à sessão HTTP (`IHttpContextAccessor`) para ler o token JWT. A sessão é específica de cada pedido HTTP. Um `Singleton` viveria para sempre e poderia partilhar estado entre diferentes utilizadores. `Scoped` garante que cada pedido HTTP tem a sua própria instância, com acesso à sua sessão.

---

## Nível 4 — Carrinho e AJAX

**P: Como funciona o carrinho, da interface até à base de dados?**
R:
1. O utilizador clica "Adicionar" num produto → o JavaScript faz `fetch('POST /Cart/Add', body: {id, qty, token})`.
2. O `CartController.Add()` chama `OrderApiClient.AddItemAsync(id, qty)`.
3. O `OrderApiClient` faz `POST api/Orders/add` com `{ productId, qty }`.
4. O `OrdersController` da API recebe, encontra o carrinho do utilizador (ou cria um novo), e adiciona/actualiza o item.
5. A resposta sobe até ao MVC que devolve `{ success: true, cartCount: N }`.
6. O JavaScript atualiza o contador no navbar: `badge.textContent = json.cartCount`.

**P: O que é um token CSRF/Antiforgery e porquê é necessário?**
R: CSRF (Cross-Site Request Forgery) é um ataque em que um site malicioso faz o browser do utilizador enviar pedidos não desejados a outro site onde ele está autenticado. O token antiforgery previne isto: o servidor gera um token aleatório e inclui-o no HTML da página. O formulário tem de enviar esse token no pedido POST. Como o site malicioso não pode ler a página do domínio alvo (SOP), não conhece o token e o pedido é rejeitado.

Neste projeto, o token é injetado numa meta tag:
```html
<meta name="request-verification-token" content="@tokens.RequestToken" />
```
O JavaScript lê-o assim:
```javascript
const token = document.querySelector('meta[name="request-verification-token"]')?.content;
```

**P: Como é que o filtro de produtos funciona sem recarregar a página?**
R: O formulário de filtros tem um listener no evento `submit`. Em vez de submeter o formulário normalmente, chama `e.preventDefault()` e usa `fetch` para ir a `GET /Products/Filter?...` com os parâmetros. O servidor devolve HTML parcial (a partial view `_ProductCard.cshtml`). O JS substitui o conteúdo do container: `container.innerHTML = html`. Por fim, re-aplica os event listeners dos botões de carrinho e favoritos à nova HTML.

**P: O que é `[ValidateAntiForgeryToken]` e o que acontece se o token falhar?**
R: É um atributo do MVC que valida o token antiforgery em pedidos POST. Se o token estiver em falta ou inválido, o ASP.NET retorna automaticamente um erro 400 (Bad Request) antes de entrar na ação do controller. Isto protege contra ataques CSRF.

---

## Nível 5 — Entity Framework, ViewModels e Upload de Imagens

**P: O que é Entity Framework Core e como é usado na Web API?**
R: É um ORM (Object-Relational Mapper) que permite trabalhar com a base de dados usando objetos C# em vez de SQL direto. O `diogoportela_SchoolBarContext` mapeia as classes de modelo (Product, Category, etc.) para tabelas SQL Server. As queries são escritas em LINQ:
```csharp
_context.Products.Where(p => p.IsActive == true).ToListAsync()
```
O EF traduz isso para SQL e executa contra o SQL Server.

**P: Qual é a diferença entre um Model e um ViewModel?**
R: Um Model representa os dados tal como estão na base de dados (e.g., `Product` com todos os campos). Um ViewModel é uma classe desenhada especificamente para uma view — pode combinar dados de vários modelos, ter campos de UI (como `IFormFile` para upload), e omitir campos que a view não precisa. Por exemplo, `ProductsViewModel` tem `ImageFile` que não existe na base de dados.

**P: Como funciona o upload de imagem de um produto?**
R:
1. O formulário tem `enctype="multipart/form-data"` e um campo `<input type="file" asp-for="ImageFile">`.
2. O MVC faz bind do ficheiro para `ProductsViewModel.ImageFile` (do tipo `IFormFile`).
3. Após guardar o produto, se `ImageFile != null && ImageFile.Length > 0`, chama-se `UploadProductImageAsync(id, file)`.
4. Este método cria um `MultipartFormDataContent`, abre o stream do ficheiro com `file.OpenReadStream()`, e faz `POST api/ProductsAdmin/{id}/upload-image`.
5. A API guarda o ficheiro e devolve o caminho `{ prodId, imgPath }`.

**P: O que é `IFormFile` e como difere de um simples `string`?**
R: `IFormFile` é uma interface do ASP.NET Core que representa um ficheiro enviado num formulário multipart. Dá acesso ao stream de bytes do ficheiro (`OpenReadStream()`), nome (`FileName`), tipo MIME (`ContentType`) e tamanho (`Length`). É necessário porque um ficheiro não é texto — é dados binários que precisam de ser lidos como stream.

**P: O que é `asp-for` nas views Razor e como funciona?**
R: `asp-for` é um Tag Helper do ASP.NET Core que gera automaticamente os atributos HTML corretos (`name`, `id`, `type`, `value`) com base na propriedade do modelo. Por exemplo:
```html
<input asp-for="Name" class="form-control" />
```
Gera: `<input name="Name" id="Name" type="text" value="NomeAtual" class="form-control">`. Também integra validação e binding automático no POST.

---

## Nível 6 — Menus, Bookings e Lógica Avançada

**P: Como é calculada a semana atual no menu semanal?**
R: A partir de hoje, calcula-se o Monday da semana:
```csharp
var today = DateOnly.FromDateTime(DateTime.Today);
var start = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
```
`DayOfWeek.Monday` é `1`, então `-(int)today.DayOfWeek + 1` volta ao Monday anterior. Se o input for sábado ou domingo, avança para o Monday seguinte. São gerados exatamente 5 dias (Seg–Sex) com `Enumerable.Range(0, 5)`.

**P: Como é que a aplicação sabe quais dias já têm reserva?**
R: No `MenuController.Index()`, se o utilizador estiver autenticado, é feita uma chamada a `BookingApiClient.GetMyBookingsAsync()` que vai a `GET api/LunchBookings/me`. As datas das reservas são guardadas num `HashSet<DateOnly>` chamado `BookedDates`. A view verifica `BookedDates.Contains(day.Date)` para cada dia e, se contiver, mostra o badge verde e desativa os botões.

**P: Porquê usar `DateOnly` em vez de `DateTime`?**
R: `DateOnly` representa apenas uma data sem hora (e sem timezone), tornando as comparações mais simples e seguras. `DateTime` pode causar erros subtis com timezones — por exemplo, dois instantes no mesmo dia mas em timezones diferentes podem comparar como datas diferentes. Para menus e reservas, só a data importa.

**P: O que é um `HashSet<T>` e porquê é usado para os favoritos e as datas reservadas?**
R: `HashSet<T>` é uma coleção que não permite duplicados e tem lookup em O(1) — verificar se um elemento existe é muito rápido, independentemente do tamanho da coleção. É ideal para `Contains()`. Uma `List<T>` teria O(n). Para os favoritos: `favs.Contains(p.ProdId)` por cada produto. Para as reservas: `bookedDates.Contains(day)` por cada dia da semana.

---

## Nível 7 — Sessão, Serialização e Edge Cases

**P: Como é que os favoritos são persistidos entre pedidos HTTP?**
R: A sessão do ASP.NET só guarda `string` ou `byte[]`. Os favoritos são guardados como JSON:
```csharp
HttpContext.Session.SetString("UserFavorites", JsonSerializer.Serialize(favs));
```
E lidos assim:
```csharp
var json = HttpContext.Session.GetString("UserFavorites");
return JsonSerializer.Deserialize<HashSet<int>>(json) ?? new HashSet<int>();
```
A sessão tem timeout de 30 minutos (`IdleTimeout = TimeSpan.FromMinutes(30)`), por isso os favoritos perdem-se ao fim de 30 minutos de inatividade.

**P: O que é `??` (null-coalescing operator) e onde é usado?**
R: `??` devolve o valor do lado esquerdo se não for null, senão devolve o do lado direito. Exemplo:
```csharp
return items ?? new List<Product>();
```
Se `GetFromJsonAsync` devolver `null` (e.g., resposta vazia), usa uma lista vazia. Também usado em: `p.Price ?? 0m` — se o preço for null, usa 0.

**P: Porque é que `GetCartAsync()` verifica o token antes de chamar a API?**
R: Se o utilizador não está autenticado, não tem token na sessão. Chamar `GET api/Orders/cart` sem token devolveria 401. Em vez de lançar uma exceção, o método devolve um carrinho vazio:
```csharp
if (_ctx.HttpContext?.Session.GetString("JwtToken") == null)
    return new OrderDto { Items = Array.Empty<OrderItemDto>(), Subtotal = 0m, Total = 0m };
```
Isto permite que a navbar mostre "0" no carrinho mesmo para utilizadores não autenticados, sem erros.

**P: O que é `StringComparison.OrdinalIgnoreCase` e porquê usá-lo nos filtros?**
R: `OrdinalIgnoreCase` compara strings byte a byte ignorando maiúsculas/minúsculas, usando as regras de comparação simples da plataforma (sem considerar regras culturais). É o mais seguro e rápido para comparações de dados que não dependem de locale — como nomes de categorias ou pesquisas. Usar `CurrentCultureIgnoreCase` poderia dar resultados inconsistentes entre diferentes sistemas operativos.

**P: Como funciona o switch expression no filtro de preços?**
R: C# 8 switch expressions são uma forma concisa de pattern matching:
```csharp
list = q.PriceRange switch
{
    "lt5"   => list.Where(p => (p.Price ?? 0m) < 5m),
    "5to10" => list.Where(p => (p.Price ?? 0m) >= 5m && (p.Price ?? 0m) <= 10m),
    "gt10"  => list.Where(p => (p.Price ?? 0m) > 10m),
    _       => list
};
```
O `_` é o caso default (wildcard). Em vez de `if/else if/else`, é mais legível. O resultado é atribuído diretamente a `list`.

---

## Nível 8 — Segurança e Boas Práticas

**P: Porque é que `DangerousAcceptAnyServerCertificateValidator` é um problema e o que deveria ser em produção?**
R: Este handler aceita qualquer certificado TLS, incluindo certificados autoassinados e inválidos. Significa que não há proteção contra ataques man-in-the-middle — um atacante poderia interceptar o tráfego sem ser detetado. Em produção, deveria ser removido e o servidor da API ter um certificado válido de uma CA de confiança.

**P: O que poderia acontecer se não usássemos tokens antiforgery no formulário de login?**
R: Um atacante poderia criar uma página num outro domínio com um formulário apontando para `/Auth/Login`. Quando o utilizador visita essa página, o formulário é submetido automaticamente e, se o utilizador já tiver sessão, pode ser redirecionado indesejadamente. Com o token antiforgery, como o site malicioso não consegue ler o token da página de login, o pedido seria rejeitado.

**P: Porque é que a chave JWT está no `appsettings.json` e não no código?**
R: Separar configuração de código permite alterar a chave sem recompilar. Também evita que a chave apareça no repositório de código (se o ficheiro for excluído do git). Em produção, deveria ser usada uma variável de ambiente ou um serviço de secrets (e.g., Azure Key Vault) em vez do `appsettings.json`.

**P: O que faz `Cookie.HttpOnly = true` na configuração da sessão?**
R: Marca o cookie da sessão como `HttpOnly`, o que impede que JavaScript do browser aceda ao cookie via `document.cookie`. Isto protege contra ataques XSS (Cross-Site Scripting) — um script injetado maliciosamente não consegue roubar o ID de sessão.

**P: O que é CORS e porquê está configurado na API?**
R: CORS (Cross-Origin Resource Sharing) é um mecanismo de segurança dos browsers que bloqueia pedidos HTTP entre domínios/portas diferentes por default. A API corre em `localhost:7234` e a MVC em `localhost:7223` — são origens diferentes. A API tem de declarar explicitamente que permite pedidos de `localhost:7223`. Sem CORS configurado, o browser recusaria as respostas da API.

---

## Nível 9 — Arquitetura e Decisões de Design

**P: Porque é que os ViewModels têm propriedades diferentes dos Models da API?**
R: Os Models da API refletem a estrutura da base de dados. Os ViewModels são moldados para o que a view precisa:
- `ProductsViewModel` tem `ImageFile` (IFormFile) que não existe na BD.
- `ProductsViewModel` tem `CategoryName` (string resolvida) em vez de só `Cat_Id`.
- `ProductsPageViewModel` tem estado de filtro (`Search`, `PriceRange`, etc.) que não é persistido.
Esta separação evita expor toda a estrutura da base de dados ao front-end e mantém as views simples.

**P: Porque é que o `ToCard()` no ProductsController resolve o nome da categoria em vez de o fazer na view?**
R: O controller recebe uma lista de categorias da API uma vez e resolve todos os nomes numa só passagem com LINQ (`FirstOrDefault(c => c.CatId == p.CatId)?.Name`). Se a view tivesse de resolver o nome, precisaria de aceder aos dados das categorias de outra forma — possivelmente chamando a API para cada produto, o que seria N+1 queries. Centralizar no controller é mais eficiente.

**P: Porque é que `ApplyFilters` recebe `IEnumerable<Product>` em vez de `IQueryable<Product>`?**
R: `IQueryable` permite que o filtro seja traduzido para SQL e executado na base de dados. Mas aqui já se foi buscar todos os produtos à API (que é um serviço HTTP externo, não uma BD local) e temos uma lista em memória. `IEnumerable` é o correto para filtrar dados já em memória. Usar `IQueryable` localmente não teria vantagem e adicionaria complexidade desnecessária.

**P: Como poderia o projeto ser melhorado para escalar para produção?**
R: Várias melhorias seriam necessárias:
1. **Remover** `DangerousAcceptAnyServerCertificateValidator` e usar certificados TLS válidos.
2. **Mover** a `SigningKey` JWT para variáveis de ambiente ou Azure Key Vault.
3. **Substituir** `AddDistributedMemoryCache()` por Redis ou SQL Server para sessões distribuídas (para suportar múltiplos servidores).
4. **Adicionar** `[Required]`, `[MaxLength]`, `[EmailAddress]` aos models para validação robusta.
5. **Externalizar** o `BaseAddress` da API para configuração em vez de estar hardcoded no `Program.cs`.
6. **Implementar** refresh tokens para não obrigar o utilizador a fazer login de 30 em 30 minutos.
