using AutoMapper;
using TravelSystem.Application.DTOs.Auth;
using TravelSystem.Application.DTOs.Flight;
using TravelSystem.Application.DTOs.Hotel;
using TravelSystem.Application.DTOs.Itinerary;
using TravelSystem.Domain.Entities;

namespace TravelSystem.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User
        CreateMap<User, UserProfileDto>()
            .ForMember(d => d.Role, opt => opt.Ignore()); // Role resolved separately

        // Itinerary
        CreateMap<Itinerary, ItineraryDto>()
            .ForMember(d => d.TotalDays, opt => opt.MapFrom(s => (s.EndDate - s.StartDate).Days + 1))
            .ForMember(d => d.StopsCount, opt => opt.MapFrom(s => s.Stops.Count));

        CreateMap<ItineraryStop, ItineraryStopDto>();
        CreateMap<CreateStopRequest, ItineraryStop>();

        CreateMap<ItineraryAttraction, ItineraryAttractionDto>();
        CreateMap<ItineraryExpense, ItineraryExpenseDto>();

        // Hotel
        CreateMap<Hotel, HotelDto>()
            .ForMember(d => d.LowestPrice, opt =>
                opt.MapFrom(s => s.Rooms.Where(r => r.IsAvailable).Min(r => (decimal?)r.PricePerNight)))
            .ForMember(d => d.CurrencyCode, opt =>
                opt.MapFrom(s => s.Rooms.Select(r => r.CurrencyCode).FirstOrDefault()));

        CreateMap<Hotel, HotelDetailDto>();
        CreateMap<HotelRoom, HotelRoomDto>();

        // Flight
        CreateMap<Flight, FlightDto>();
        CreateMap<FlightAlert, FlightAlertDto>();

        // Booking
        CreateMap<Booking, BookingDto>()
            .ForMember(d => d.HotelName, opt => opt.MapFrom(s => s.Hotel != null ? s.Hotel.Name : null))
            .ForMember(d => d.FlightNumber, opt => opt.MapFrom(s => s.Flight != null ? s.Flight.FlightNumber : null));
    }
}
