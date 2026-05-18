using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Domain.Entities;

namespace TravelSystem.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Itinerary> Itineraries => Set<Itinerary>();
    public DbSet<ItineraryStop> ItineraryStops => Set<ItineraryStop>();
    public DbSet<ItineraryAttraction> ItineraryAttractions => Set<ItineraryAttraction>();
    public DbSet<ItineraryExpense> ItineraryExpenses => Set<ItineraryExpense>();
    public DbSet<AiChatMessage> AiChatMessages => Set<AiChatMessage>();
    public DbSet<Hotel> Hotels => Set<Hotel>();
    public DbSet<HotelRoom> HotelRooms => Set<HotelRoom>();
    public DbSet<Flight> Flights => Set<Flight>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<FlightAlert> FlightAlerts => Set<FlightAlert>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Rename Identity tables for clarity
        builder.Entity<User>().ToTable("Users");
        builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        // User
        builder.Entity<User>(e =>
        {
            e.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            e.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            e.Property(u => u.PreferredLanguage).HasMaxLength(10).HasDefaultValue("pt-AO");
            e.Property(u => u.AvatarUrl).HasMaxLength(500);
        });

        // Itinerary
        builder.Entity<Itinerary>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Title).HasMaxLength(200).IsRequired();
            e.Property(i => i.Destination).HasMaxLength(200).IsRequired();
            e.Property(i => i.CountryCode).HasMaxLength(3);
            e.Property(i => i.CurrencyCode).HasMaxLength(3);
            e.Property(i => i.Budget).HasColumnType("decimal(15,2)");
            e.HasOne(i => i.User).WithMany(u => u.Itineraries)
                .HasForeignKey(i => i.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(i => i.Stops).WithOne(s => s.Itinerary)
                .HasForeignKey(s => s.ItineraryId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(i => i.Attractions).WithOne(a => a.Itinerary)
                .HasForeignKey(a => a.ItineraryId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(i => i.ChatHistory).WithOne(m => m.Itinerary)
                .HasForeignKey(m => m.ItineraryId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(i => i.Expenses).WithOne(ex => ex.Itinerary)
                .HasForeignKey(ex => ex.ItineraryId).OnDelete(DeleteBehavior.Cascade);
        });

        // ItineraryStop
        builder.Entity<ItineraryStop>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Name).HasMaxLength(200).IsRequired();
            e.Property(s => s.Address).HasMaxLength(500);
            e.HasIndex(s => new { s.ItineraryId, s.DayNumber, s.OrderIndex });
        });

        // ItineraryAttraction
        builder.Entity<ItineraryAttraction>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.PlaceId).HasMaxLength(200).IsRequired();
            e.Property(a => a.Name).HasMaxLength(200).IsRequired();
            e.Property(a => a.ImageUrl).HasMaxLength(500);
        });

        // ItineraryExpense
        builder.Entity<ItineraryExpense>(e =>
        {
            e.HasKey(ex => ex.Id);
            e.Property(ex => ex.Amount).HasColumnType("decimal(15,2)").IsRequired();
            e.Property(ex => ex.Category).HasMaxLength(100).IsRequired();
            e.Property(ex => ex.CurrencyCode).HasMaxLength(3);
        });

        // AiChatMessage
        builder.Entity<AiChatMessage>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Role).HasMaxLength(20).IsRequired();
            e.Property(m => m.Content).HasColumnType("TEXT").IsRequired();
        });

        // Hotel
        builder.Entity<Hotel>(e =>
        {
            e.HasKey(h => h.Id);
            e.Property(h => h.Name).HasMaxLength(300).IsRequired();
            e.Property(h => h.ExternalId).HasMaxLength(100);
            e.Property(h => h.Provider).HasMaxLength(50);
            e.Property(h => h.Address).HasMaxLength(500);
            e.Property(h => h.City).HasMaxLength(200);
            e.Property(h => h.CountryCode).HasMaxLength(3);
            e.Property(h => h.ImageUrl).HasMaxLength(500);
            e.Property(h => h.Amenities)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                );
            e.HasMany(h => h.Rooms).WithOne(r => r.Hotel)
                .HasForeignKey(r => r.HotelId).OnDelete(DeleteBehavior.Cascade);
        });

        // HotelRoom
        builder.Entity<HotelRoom>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.RoomType).HasMaxLength(100).IsRequired();
            e.Property(r => r.PricePerNight).HasColumnType("decimal(10,2)");
            e.Property(r => r.CurrencyCode).HasMaxLength(3);
        });

        // Flight
        builder.Entity<Flight>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.Airline).HasMaxLength(200).IsRequired();
            e.Property(f => f.FlightNumber).HasMaxLength(20).IsRequired();
            e.Property(f => f.OriginCode).HasMaxLength(3).IsRequired();
            e.Property(f => f.DestinationCode).HasMaxLength(3).IsRequired();
            e.Property(f => f.Price).HasColumnType("decimal(10,2)");
            e.Property(f => f.CurrencyCode).HasMaxLength(3);
        });

        // Booking
        builder.Entity<Booking>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.TotalPrice).HasColumnType("decimal(10,2)");
            e.Property(b => b.CurrencyCode).HasMaxLength(3);
            e.Property(b => b.ConfirmationNumber).HasMaxLength(100);
            e.HasOne(b => b.User).WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(b => b.Hotel).WithMany(h => h.Bookings)
                .HasForeignKey(b => b.HotelId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(b => b.Flight).WithMany(f => f.Bookings)
                .HasForeignKey(b => b.FlightId).OnDelete(DeleteBehavior.SetNull);
        });

        // FlightAlert
        builder.Entity<FlightAlert>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.OriginCode).HasMaxLength(3).IsRequired();
            e.Property(a => a.DestinationCode).HasMaxLength(3).IsRequired();
            e.Property(a => a.TargetPrice).HasColumnType("decimal(10,2)");
            e.Property(a => a.CurrencyCode).HasMaxLength(3);
            e.HasOne(a => a.User).WithMany(u => u.FlightAlerts)
                .HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
