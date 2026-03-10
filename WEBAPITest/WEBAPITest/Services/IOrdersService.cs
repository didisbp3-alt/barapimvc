using DTO_MVCAPIContracts.Contracts;

namespace WEBAPITest.Services;

public interface IOrdersService
{
    // Cart (open order)
    Task<OperationResult<OrderDto>> GetOrCreateCartAsync(int userId);
    Task<OperationResult<OrderDto>> AddItemAsync(int userId, int productId, int qty);
    Task<OperationResult<OrderDto>> UpdateItemAsync(int userId, int productId, int qty);
    Task<OperationResult<OrderDto>> RemoveItemAsync(int userId, int productId);

    // Checkout & history
    Task<OperationResult<int>> CreateOrderAsync(int userId, OrderDto dto);
    Task<OperationResult<IEnumerable<OrderDto>>> GetUserOrdersAsync(int userId);
    Task<OperationResult<OrderDto>> GetOrderByIdAsync(int userId, int orderId);
}