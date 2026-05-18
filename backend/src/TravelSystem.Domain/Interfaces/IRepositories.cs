using TravelSystem.Domain.Entities;
using System.Linq.Expressions;

namespace TravelSystem.Domain.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
}

public interface IUnitOfWork : IDisposable
{
    IItineraryRepository Itineraries { get; }
    IHotelRepository Hotels { get; }
    IFlightRepository Flights { get; }
    IBookingRepository Bookings { get; }
    IFlightAlertRepository FlightAlerts { get; }

    Task<int> CommitAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}

public interface IItineraryRepository : IRepository<Itinerary>
{
    Task<IEnumerable<Itinerary>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Itinerary?> GetWithStopsAsync(Guid id, CancellationToken ct = default);
    Task<Itinerary?> GetWithChatHistoryAsync(Guid id, CancellationToken ct = default);
    Task<Itinerary?> GetFullItineraryAsync(Guid id, CancellationToken ct = default);
}

public interface IHotelRepository : IRepository<Hotel>
{
    Task<IEnumerable<Hotel>> SearchAsync(string city, DateTime checkIn, DateTime checkOut, int guests, CancellationToken ct = default);
    Task<Hotel?> GetWithRoomsAsync(Guid id, CancellationToken ct = default);
}

public interface IFlightRepository : IRepository<Flight>
{
    Task<IEnumerable<Flight>> SearchAsync(string origin, string destination, DateTime departure, int passengers, CancellationToken ct = default);
}

public interface IBookingRepository : IRepository<Booking>
{
    Task<IEnumerable<Booking>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Booking?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);
}

public interface IFlightAlertRepository : IRepository<FlightAlert>
{
    Task<IEnumerable<FlightAlert>> GetActiveAlertsAsync(CancellationToken ct = default);
    Task<IEnumerable<FlightAlert>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
}
