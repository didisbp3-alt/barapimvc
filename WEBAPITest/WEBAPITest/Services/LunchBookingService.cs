using DTO_MVCAPIContracts.Contracts;
using Microsoft.EntityFrameworkCore;
using WEBAPITest.Data;
using WEBAPITest.Models;

namespace WEBAPITest.Services;

public class LunchBookingService : ILunchBookingService
{
    private readonly diogoportela_SchoolBarContext _db;

    public LunchBookingService(diogoportela_SchoolBarContext db)
    {
        _db = db;
    }

    // Regras aplicadas:
    // - Marcação no próprio dia só até 10:00
    // - Cancelamento no próprio dia só até 09:30 (implementado no controller/service de cancel)
    // - Limite de vagas por menu (AvailableSeats)
    // - Um utilizador não pode ter mais do que uma marcação no mesmo dia
    public async Task<OperationResult> BookLunchAsync(int userId, int menuId)
    {
        var menu = await _db.Menus.FirstOrDefaultAsync(m => m.MId == menuId);
        if (menu is null)
            return OperationResult.Fail("Menu não encontrado.");

        if (menu.Date is null)
            return OperationResult.Fail("Menu inválido (data não definida).");

        var now = DateTime.Now; // usa horário do servidor (ajusta se precisares de UTC)
        var menuDate = menu.Date.Value.Date;

        // regra: marcação para o próprio dia só até 10:00
        if (menuDate == now.Date && now.TimeOfDay > TimeSpan.FromHours(10))
            return OperationResult.Fail("Marcação para o próprio dia apenas até às 10:00.");

        // já existe marcação do utilizador para esse dia?
        var hasBooking = await _db.LunchBookings
            .AnyAsync(lb => lb.UId == userId && lb.MIdNavigation.Date.HasValue && lb.MIdNavigation.Date.Value.Date == menuDate);

        if (hasBooking)
            return OperationResult.Fail("Já existe uma marcação sua para esse dia.");

        if (menu.AvailableSeats <= 0)
            return OperationResult.Fail("Não existem vagas disponíveis para este menu.");

        // cria marcação
        var booking = new LunchBooking
        {
            UId = userId,
            MId = menu.MId,
            Date = DateOnly.FromDateTime(menu.Date.Value),
            CreationDate = DateOnly.FromDateTime(now)
        };

        // actualiza contadores
        menu.UsedSeats = (menu.UsedSeats ?? 0) + 1;
        // AvailableSeats é NOT NULL no modelo scaffold
        menu.AvailableSeats = menu.AvailableSeats - 1;

        _db.LunchBookings.Add(booking);
        await _db.SaveChangesAsync();

        return OperationResult.Ok("Marcação efetuada com sucesso.");
    }

    public async Task<OperationResult> CancelBookingAsync(int userId, int bookingId)
    {
        var booking = await _db.LunchBookings
            .Include(lb => lb.MIdNavigation)
            .FirstOrDefaultAsync(lb => lb.LId == bookingId && lb.UId == userId);

        if (booking is null)
            return OperationResult.Fail("Marcação não encontrada.");

        var now = DateTime.Now;
        var bookingDate = booking.MIdNavigation.Date?.Date;

        if (bookingDate is null)
            return OperationResult.Fail("Menu da marcação inválido.");

        // regra: cancelamento no próprio dia apenas até 09:30
        if (bookingDate.Value.Date == now.Date && now.TimeOfDay > new TimeSpan(9, 30, 0))
            return OperationResult.Fail("Cancelamento no próprio dia apenas até às 09:30.");

        // actualiza contadores do menu
        var menu = booking.MIdNavigation;
        menu.UsedSeats = (menu.UsedSeats ?? 1) - 1;
        menu.AvailableSeats = menu.AvailableSeats + 1;

        _db.LunchBookings.Remove(booking);
        await _db.SaveChangesAsync();

        return OperationResult.Ok("Marcação cancelada com sucesso.");
    }

    public async Task<IEnumerable<LunchBookingDto>> GetUserBookingsAsync(int userId)
    {
        var items = await _db.LunchBookings
            .Include(lb => lb.MIdNavigation)
            .Where(lb => lb.UId == userId)
            .OrderByDescending(lb => lb.LId)
            .ToListAsync();

        return items.Select(lb => new LunchBookingDto
        {
            Id = lb.LId,
            UserId = lb.UId,
            MenuId = lb.MId,
            Date = lb.MIdNavigation.Date?.Date ?? DateTime.MinValue,
            CreatedAt = lb.CreationDate.HasValue ? lb.CreationDate.Value.ToDateTime(TimeOnly.MinValue) : DateTime.MinValue
        });
    }
}