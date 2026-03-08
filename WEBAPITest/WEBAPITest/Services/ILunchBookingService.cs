using DTO_MVCAPIContracts.Contracts;

namespace WEBAPITest.Services;

public interface ILunchBookingService
{
    Task<OperationResult> BookLunchAsync(int userId, int menuId);
    Task<OperationResult> CancelBookingAsync(int userId, int bookingId);
    Task<IEnumerable<LunchBookingDto>> GetUserBookingsAsync(int userId);
}