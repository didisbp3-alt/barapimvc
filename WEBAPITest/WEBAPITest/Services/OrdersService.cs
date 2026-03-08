using DTO_MVCAPIContracts.Contracts;
using Microsoft.EntityFrameworkCore;
using WEBAPITest.Data;
using WEBAPITest.Models;

namespace WEBAPITest.Services;

public class OrdersService : IOrdersService
{
    private readonly diogoportela_SchoolBarContext _db;
    private const string CartStatus = "Cart";

    public OrdersService(diogoportela_SchoolBarContext db) => _db = db;

    #region Cart
    public async Task<OperationResult<OrderDto>> GetOrCreateCartAsync(int userId)
    {
        var cart = await GetCartEntity(userId) ?? await CreateCart(userId);
        Recalc(cart);
        return OperationResult<OrderDto>.Ok(MapOrderToDto(cart), "OK");
    }

    public async Task<OperationResult<OrderDto>> AddItemAsync(int userId, int productId, int qty)
    {
        if (qty <= 0) return OperationResult<OrderDto>.Fail("Quantidade inválida.");

        var cart = await GetCartEntity(userId) ?? await CreateCart(userId);
        cart.Items ??= new List<OrderItem>();

        var product = await _db.Products.FirstOrDefaultAsync(p => p.ProdId == productId);
        if (product == null) return OperationResult<OrderDto>.Fail("Produto não encontrado.");

        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
        {
            cart.Items.Add(new OrderItem
            {
                ProductId = product.ProdId,
                Quantity = qty,
                UnitPrice = product.Price ?? 0m
            });
        }
        else
        {
            item.Quantity += qty;
        }

        Recalc(cart);
        await _db.SaveChangesAsync();
        return OperationResult<OrderDto>.Ok(MapOrderToDto(cart), "OK");
    }

    public async Task<OperationResult<OrderDto>> UpdateItemAsync(int userId, int productId, int qty)
    {
        if (qty < 0) return OperationResult<OrderDto>.Fail("Quantidade inválida.");

        var cart = await GetCartEntity(userId);
        if (cart == null) return OperationResult<OrderDto>.Fail("Carrinho não encontrado.");

        cart.Items ??= new List<OrderItem>();
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null) return OperationResult<OrderDto>.Fail("Item não encontrado.");

        if (qty == 0) cart.Items.Remove(item);
        else item.Quantity = qty;

        Recalc(cart);
        await _db.SaveChangesAsync();
        return OperationResult<OrderDto>.Ok(MapOrderToDto(cart), "OK");
    }

    public async Task<OperationResult<OrderDto>> RemoveItemAsync(int userId, int productId)
    {
        var cart = await GetCartEntity(userId);
        if (cart == null) return OperationResult<OrderDto>.Fail("Carrinho não encontrado.");

        cart.Items ??= new List<OrderItem>();
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null) return OperationResult<OrderDto>.Fail("Item não encontrado.");

        cart.Items.Remove(item);
        Recalc(cart);
        await _db.SaveChangesAsync();
        return OperationResult<OrderDto>.Ok(MapOrderToDto(cart), "OK");
    }
    #endregion

    #region CreateOrderAsync
    public async Task<OperationResult<int>> CreateOrderAsync(int userId, OrderDto dto)
    {
        if (dto.Items == null || !dto.Items.Any())
            return OperationResult<int>.Fail("Encomenda sem items.");

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var order = new Orders
            {
                UserId = userId,
                Status = "Processing",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Total = 0m,
                Notes = dto.Notes,
                Items = new List<OrderItem>()
            };

            decimal total = 0m;

            foreach (var it in dto.Items)
            {
                if (it.Quantity <= 0)
                {
                    await tx.RollbackAsync();
                    return OperationResult<int>.Fail($"Quantidade inválida para o produto {it.ProductId}.");
                }

                var product = await _db.Products.FirstOrDefaultAsync(p => p.ProdId == it.ProductId);
                if (product == null)
                {
                    await tx.RollbackAsync();
                    return OperationResult<int>.Fail($"Produto {it.ProductId} não encontrado.");
                }

                var curStock = product.CurStock ?? 0;
                if (curStock < it.Quantity)
                {
                    await tx.RollbackAsync();
                    return OperationResult<int>.Fail($"Stock insuficiente para o produto '{product.Name}' (id={product.ProdId}).");
                }

                product.CurStock = curStock - it.Quantity;

                var unitPrice = product.Price ?? 0m;
                total += unitPrice * it.Quantity;

                order.Items.Add(new OrderItem
                {
                    ProductId = product.ProdId,
                    Quantity = it.Quantity,
                    UnitPrice = unitPrice
                });
            }

            order.Total = total;

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            await tx.CommitAsync();

            return OperationResult<int>.Ok(order.OId, "Encomenda criada com sucesso.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return OperationResult<int>.Fail("Erro ao criar encomenda: " + ex.Message);
        }
    }
    #endregion

    #region GetUserOrdersAsync
    public async Task<OperationResult<IEnumerable<OrderDto>>> GetUserOrdersAsync(int userId)
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var list = orders.Select(MapOrderToDto).ToList();
        return OperationResult<IEnumerable<OrderDto>>.Ok(list, "OK");
    }
    #endregion

    #region GetOrderByIdAsync
    public async Task<OperationResult<OrderDto>> GetOrderByIdAsync(int userId, int orderId)
    {
        var o = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.OId == orderId && x.UserId == userId);

        if (o == null) return OperationResult<OrderDto>.Fail("Encomenda não encontrada.");

        var dto = MapOrderToDto(o);
        return OperationResult<OrderDto>.Ok(dto, "OK");
    }
    #endregion

    #region Helpers
    private async Task<Orders?> GetCartEntity(int userId)
    {
        return await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == CartStatus);
    }

    private async Task<Orders> CreateCart(int userId)
    {
        var cart = new Orders
        {
            UserId = userId,
            Status = CartStatus,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>()
        };
        _db.Orders.Add(cart);
        await _db.SaveChangesAsync();
        return cart;
    }

    private void Recalc(Orders order)
    {
        order.Items ??= new List<OrderItem>();
        order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        order.UpdatedAt = DateTime.UtcNow;
    }

    private static OrderDto MapOrderToDto(Orders o) =>
        new OrderDto
        {
            OrderId = o.OId,
            Total = o.Total,
            Subtotal = (o.Items ?? Enumerable.Empty<OrderItem>()).Sum(i => i.UnitPrice * i.Quantity),
            Status = o.Status,
            CreatedAt = o.CreatedAt,
            Notes = o.Notes,
            Items = (o.Items ?? Enumerable.Empty<OrderItem>()).Select(i => new OrderItemDto
            {
                ProductId = i.ProductId.GetValueOrDefault(),
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
    #endregion
}