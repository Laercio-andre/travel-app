using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TravelSystem.Domain.Entities;
using TravelSystem.Domain.Interfaces;

namespace TravelSystem.Infrastructure.Data.Repositories;

// ── Base Repository ──────────────────────────────────────────────────────────

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _ctx;
    protected readonly DbSet<T> _set;

    public Repository(AppDbContext ctx)
    {
        _ctx = ctx;
        _set = ctx.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _set.FindAsync([id], ct);

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
        => await _set.ToListAsync(ct);

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _set.Where(predicate).ToListAsync(ct);

    public async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await _set.AddAsync(entity, ct);
        return entity;
    }

    public Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        _set.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        _set.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _set.AnyAsync(predicate, ct);

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        => predicate is null ? await _set.CountAsync(ct) : await _set.CountAsync(predicate, ct);
}

// ── Itinerary Repository ─────────────────────────────────────────────────────

public class ItineraryRepository : Repository<Itinerary>, IItineraryRepository
{
    public ItineraryRepository(AppDbContext ctx) : base(ctx) { }

    public async Task<IEnumerable<Itinerary>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _ctx.Itineraries
            .Where(i => i.UserId == userId)
            .Include(i => i.Stops)
            .OrderByDescending(i => i.UpdatedAt)
            .ToListAsync(ct);

    public async Task<Itinerary?> GetWithStopsAsync(Guid id, CancellationToken ct = default)
        => await _ctx.Itineraries
            .Include(i => i.Stops)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<Itinerary?> GetWithChatHistoryAsync(Guid id, CancellationToken ct = default)
        => await _ctx.Itineraries
            .Include(i => i.ChatHistory.OrderBy(m => m.CreatedAt))
            .Include(i => i.Stops)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<Itinerary?> GetFullItineraryAsync(Guid id, CancellationToken ct = default)
        => await _ctx.Itineraries
            .Include(i => i.Stops.OrderBy(s => s.DayNumber).ThenBy(s => s.OrderIndex))
            .Include(i => i.Attractions)
            .Include(i => i.ChatHistory.OrderBy(m => m.CreatedAt))
            .Include(i => i.Expenses.OrderBy(e => e.Date))
            .FirstOrDefaultAsync(i => i.Id == id, ct);
}

// ── Hotel Repository ─────────────────────────────────────────────────────────

public class HotelRepository : Repository<Hotel>, IHotelRepository
{
    public HotelRepository(AppDbContext ctx) : base(ctx) { }

    public async Task<IEnumerable<Hotel>> SearchAsync(string city, DateTime checkIn, DateTime checkOut, int guests, CancellationToken ct = default)
        => await _ctx.Hotels
            .Include(h => h.Rooms)
            .Where(h => h.City.ToLower().Contains(city.ToLower()) &&
                        h.Rooms.Any(r => r.IsAvailable && r.MaxGuests >= guests))
            .ToListAsync(ct);

    public async Task<Hotel?> GetWithRoomsAsync(Guid id, CancellationToken ct = default)
        => await _ctx.Hotels.Include(h => h.Rooms).FirstOrDefaultAsync(h => h.Id == id, ct);
}

// ── Flight Repository ────────────────────────────────────────────────────────

public class FlightRepository : Repository<Flight>, IFlightRepository
{
    public FlightRepository(AppDbContext ctx) : base(ctx) { }

    public async Task<IEnumerable<Flight>> SearchAsync(string origin, string destination, DateTime departure, int passengers, CancellationToken ct = default)
        => await _ctx.Flights
            .Where(f => f.OriginCode == origin.ToUpper() &&
                        f.DestinationCode == destination.ToUpper() &&
                        f.DepartureAt.Date == departure.Date &&
                        f.SeatsAvailable >= passengers)
            .OrderBy(f => f.Price)
            .ToListAsync(ct);
}

// ── Booking Repository ───────────────────────────────────────────────────────

public class BookingRepository : Repository<Booking>, IBookingRepository
{
    public BookingRepository(AppDbContext ctx) : base(ctx) { }

    public async Task<IEnumerable<Booking>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _ctx.Bookings
            .Include(b => b.Hotel)
            .Include(b => b.Flight)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);

    public async Task<Booking?> GetWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await _ctx.Bookings
            .Include(b => b.Hotel).ThenInclude(h => h!.Rooms)
            .Include(b => b.Flight)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
}

// ── FlightAlert Repository ───────────────────────────────────────────────────

public class FlightAlertRepository : Repository<FlightAlert>, IFlightAlertRepository
{
    public FlightAlertRepository(AppDbContext ctx) : base(ctx) { }

    public async Task<IEnumerable<FlightAlert>> GetActiveAlertsAsync(CancellationToken ct = default)
        => await _ctx.FlightAlerts
            .Include(a => a.User)
            .Where(a => a.IsActive)
            .ToListAsync(ct);

    public async Task<IEnumerable<FlightAlert>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _ctx.FlightAlerts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
}

// ── Unit of Work ─────────────────────────────────────────────────────────────

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _ctx;
    private IDbContextTransaction? _transaction;

    public IItineraryRepository Itineraries { get; }
    public IHotelRepository Hotels { get; }
    public IFlightRepository Flights { get; }
    public IBookingRepository Bookings { get; }
    public IFlightAlertRepository FlightAlerts { get; }

    public UnitOfWork(AppDbContext ctx)
    {
        _ctx = ctx;
        Itineraries = new ItineraryRepository(ctx);
        Hotels = new HotelRepository(ctx);
        Flights = new FlightRepository(ctx);
        Bookings = new BookingRepository(ctx);
        FlightAlerts = new FlightAlertRepository(ctx);
    }

    public async Task<int> CommitAsync(CancellationToken ct = default)
        => await _ctx.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _transaction = await _ctx.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;
        await _transaction.CommitAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;
        await _transaction.RollbackAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _ctx.Dispose();
    }
}
