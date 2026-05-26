using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TravelSystem.Domain.Entities;
using TravelSystem.Domain.Enums;

namespace TravelSystem.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var db = services.GetRequiredService<AppDbContext>();

        // Seed roles
        string[] roles = ["Admin", "PremiumTraveler", "Traveler"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        // Seed admin user
        const string adminEmail = "admin@travelsystem.ao";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new User
            {
                Email = adminEmail,
                UserName = adminEmail,
                FirstName = "Admin",
                LastName = "TravelSystem",
                EmailConfirmed = true,
                PreferredLanguage = "pt-AO"
            };

            var result = await userManager.CreateAsync(admin, "Admin@123456");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }

        const string travelerEmail = "viajante@travelsystem.ao";
        var traveler = await userManager.FindByEmailAsync(travelerEmail);
        if (traveler is null)
        {
            traveler = new User
            {
                Email = travelerEmail,
                UserName = travelerEmail,
                FirstName = "Ana",
                LastName = "Kiala",
                EmailConfirmed = true,
                PreferredLanguage = "pt-AO"
            };

            var result = await userManager.CreateAsync(traveler, "Cliente123");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(traveler, "Traveler");
        }

        await SeedAngolaTravelDataAsync(db, traveler);
    }

    private static async Task SeedAngolaTravelDataAsync(AppDbContext db, User? traveler)
    {
        var seededHotels = CreateAngolaHotels();
        if (!db.Hotels.Any())
        {
            db.Hotels.AddRange(seededHotels);
        }
        else
        {
            RepairHotelImages(db, seededHotels);
        }

        if (!db.Flights.Any())
        {
            db.Flights.AddRange(CreateAngolaFlights());
        }

        if (traveler is not null && !db.Itineraries.Any(i => i.UserId == traveler.Id))
        {
            db.Itineraries.Add(CreateSampleAngolaItinerary(traveler.Id));
        }

        await db.SaveChangesAsync();
    }

    private static List<Hotel> CreateAngolaHotels() =>
    [
        CreateHotel("ao-luanda-epic-sana", "EPIC SANA Luanda Hotel", "Rua da Missao, Ingombota", "Luanda", -8.8147, 13.2302, 5, 4.6, "Hotel central em Luanda, perto da Marginal e de zonas empresariais.", 185000, ["Wi-Fi", "Piscina", "Ginásio", "Restaurante", "Transfer"], "https://images.unsplash.com/photo-1566073771259-6a8506099945?auto=format&fit=crop&w=1200&q=80"),
        CreateHotel("ao-luanda-presidente", "Hotel Presidente Luanda", "Largo 4 de Fevereiro", "Luanda", -8.8064, 13.2417, 4, 4.2, "Alojamento clássico junto à Baía de Luanda.", 142000, ["Wi-Fi", "Restaurante", "Vista para a baía", "Estacionamento"], "https://images.unsplash.com/photo-1551882547-ff40c63fe5fa?auto=format&fit=crop&w=1200&q=80"),
        CreateHotel("ao-benguela-praia-morena", "Hotel Praia Morena", "Avenida 10 de Fevereiro", "Benguela", -12.5763, 13.4055, 3, 4.1, "Base confortável para explorar Benguela, Baía Azul e Lobito.", 68000, ["Wi-Fi", "Restaurante", "Ar condicionado"], "https://images.unsplash.com/photo-1520250497591-112f2f40a3f4?auto=format&fit=crop&w=1200&q=80"),
        CreateHotel("ao-huila-serra-da-chela", "Hotel Serra da Chela", "Centro da cidade", "Lubango", -14.9186, 13.4925, 4, 4.3, "Ponto de partida para Tundavala, Cristo Rei e Serra da Leba.", 76000, ["Wi-Fi", "Restaurante", "Pequeno-almoço", "Estacionamento"], "https://images.unsplash.com/photo-1542314831-068cd1dbfeeb?auto=format&fit=crop&w=1200&q=80"),
        CreateHotel("ao-namibe-infanta", "Hotel Infotur Namibe", "Avenida Eduardo Mondlane", "Moçâmedes", -15.1961, 12.1522, 3, 4.0, "Estadia prática para visitar o Deserto do Namibe e a Praia das Miragens.", 59000, ["Wi-Fi", "Restaurante", "Ar condicionado"], "https://images.unsplash.com/photo-1564501049412-61c2a3083791?auto=format&fit=crop&w=1200&q=80"),
        CreateHotel("ao-malanje-kalandula", "Pousada das Quedas de Kalandula", "Kalandula", "Malanje", -9.0752, 16.0019, 3, 4.1, "Alojamento próximo das Quedas de Kalandula.", 62000, ["Restaurante", "Vista natural", "Estacionamento"], "https://images.unsplash.com/photo-1582719508461-905c673771fd?auto=format&fit=crop&w=1200&q=80"),
        CreateHotel("ao-cabinda-maiorca", "Hotel Maiorca", "Centro de Cabinda", "Cabinda", -5.55, 12.1976, 3, 3.9, "Opção urbana para viagens de trabalho e lazer em Cabinda.", 71000, ["Wi-Fi", "Restaurante", "Bar"], "https://images.unsplash.com/photo-1563911302283-d2bc129e7570?auto=format&fit=crop&w=1200&q=80"),
        CreateHotel("ao-huambo-ekuikui", "Hotel Ekuikui I", "Centro do Huambo", "Huambo", -12.7761, 15.7392, 4, 4.0, "Base para conhecer a cidade do Huambo e a região do Planalto Central.", 70000, ["Wi-Fi", "Restaurante", "Salas de reunião"], "https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&w=1200&q=80")
    ];

    private static Hotel CreateHotel(
        string externalId,
        string name,
        string address,
        string city,
        double latitude,
        double longitude,
        int stars,
        double guestRating,
        string description,
        decimal basePrice,
        List<string> amenities,
        string imageUrl)
    {
        var hotel = new Hotel
        {
            ExternalId = externalId,
            Provider = "seed-angola",
            Name = name,
            Address = address,
            City = city,
            CountryCode = "AO",
            Latitude = latitude,
            Longitude = longitude,
            StarRating = stars,
            GuestRating = guestRating,
            Description = description,
            Amenities = amenities,
            ImageUrl = imageUrl
        };

        hotel.Rooms.Add(new HotelRoom
        {
            RoomType = "Standard",
            Description = "Quarto duplo standard",
            MaxGuests = 2,
            PricePerNight = basePrice,
            CurrencyCode = "AOA"
        });
        hotel.Rooms.Add(new HotelRoom
        {
            RoomType = "Suite",
            Description = "Suite com pequeno-almoço incluído",
            MaxGuests = 3,
            PricePerNight = Math.Round(basePrice * 1.65m, 0),
            CurrencyCode = "AOA"
        });

        return hotel;
    }

    private static void RepairHotelImages(AppDbContext db, List<Hotel> seededHotels)
    {
        var imageMap = seededHotels.ToDictionary(h => h.ExternalId, h => h.ImageUrl);
        var externalIds = imageMap.Keys.ToList();
        foreach (var hotel in db.Hotels.Where(h => externalIds.Contains(h.ExternalId)))
        {
            if (string.IsNullOrWhiteSpace(hotel.ImageUrl) || hotel.ImageUrl.Contains("source.unsplash.com", StringComparison.OrdinalIgnoreCase))
            {
                hotel.ImageUrl = imageMap[hotel.ExternalId];
            }
        }
    }

    private static List<Flight> CreateAngolaFlights()
    {
        var start = DateTime.UtcNow.Date.AddDays(10).AddHours(8);
        return
        [
            CreateFlight("ao-flight-lad-sdd", "TAAG Angola Airlines", "DT 453", "LAD", "SDD", "Luanda", "Lubango", start, 85, 95000),
            CreateFlight("ao-flight-lad-bug", "TAAG Angola Airlines", "DT 441", "LAD", "BUG", "Luanda", "Benguela", start.AddHours(2), 70, 78000),
            CreateFlight("ao-flight-lad-msz", "TAAG Angola Airlines", "DT 461", "LAD", "MSZ", "Luanda", "Moçâmedes", start.AddDays(1), 90, 99000),
            CreateFlight("ao-flight-lad-cab", "TAAG Angola Airlines", "DT 121", "LAD", "CAB", "Luanda", "Cabinda", start.AddDays(1).AddHours(3), 65, 88000),
            CreateFlight("ao-flight-lad-nov", "TAAG Angola Airlines", "DT 575", "LAD", "NOV", "Luanda", "Huambo", start.AddDays(2), 75, 84000),
            CreateFlight("ao-flight-sdd-msz", "Fly Angola", "EQ 302", "SDD", "MSZ", "Lubango", "Moçâmedes", start.AddDays(3).AddHours(1), 45, 54000)
        ];
    }

    private static Flight CreateFlight(
        string externalId,
        string airline,
        string flightNumber,
        string originCode,
        string destinationCode,
        string originCity,
        string destinationCity,
        DateTime departureAt,
        int durationMinutes,
        decimal price) =>
        new()
        {
            ExternalId = externalId,
            Provider = "seed-angola",
            Airline = airline,
            FlightNumber = flightNumber,
            OriginCode = originCode,
            DestinationCode = destinationCode,
            OriginCity = originCity,
            DestinationCity = destinationCity,
            DepartureAt = departureAt,
            ArrivalAt = departureAt.AddMinutes(durationMinutes),
            DurationMinutes = durationMinutes,
            Stops = 0,
            CabinClass = "economy",
            Price = price,
            CurrencyCode = "AOA",
            SeatsAvailable = 24
        };

    private static Itinerary CreateSampleAngolaItinerary(Guid userId)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(14);
        var itinerary = new Itinerary
        {
            UserId = userId,
            Title = "Angola essencial: Luanda, Malanje e Huíla",
            Description = "Roteiro exemplo com património histórico, natureza e paisagens icónicas de Angola.",
            Destination = "Angola",
            CountryCode = "AO",
            Latitude = -11.2027,
            Longitude = 17.8739,
            StartDate = startDate,
            EndDate = startDate.AddDays(5),
            Budget = 850000,
            CurrencyCode = "AOA",
            Status = ItineraryStatus.Active
        };

        itinerary.Stops.Add(new ItineraryStop { Name = "Fortaleza de São Miguel", Address = "Calçada de São Miguel, Luanda", Latitude = -8.8167, Longitude = 13.2344, DayNumber = 1, OrderIndex = 0, Category = StopCategory.Attraction, DurationMinutes = 90, Notes = "História colonial e vista para a cidade." });
        itinerary.Stops.Add(new ItineraryStop { Name = "Ilha do Cabo", Address = "Ilha de Luanda", Latitude = -8.7835, Longitude = 13.2489, DayNumber = 1, OrderIndex = 1, Category = StopCategory.Restaurant, DurationMinutes = 120, Notes = "Fim de tarde e jantar junto ao mar." });
        itinerary.Stops.Add(new ItineraryStop { Name = "Miradouro da Lua", Address = "Estrada Nacional 100, Belas", Latitude = -9.4086, Longitude = 13.1362, DayNumber = 2, OrderIndex = 0, Category = StopCategory.Attraction, DurationMinutes = 75, Notes = "Paisagem erosiva a sul de Luanda." });
        itinerary.Stops.Add(new ItineraryStop { Name = "Parque Nacional da Quiçama", Address = "Bengo", Latitude = -9.75, Longitude = 13.95, DayNumber = 2, OrderIndex = 1, Category = StopCategory.Activity, DurationMinutes = 240, Notes = "Safari e contacto com natureza." });
        itinerary.Stops.Add(new ItineraryStop { Name = "Quedas de Kalandula", Address = "Kalandula, Malanje", Latitude = -9.0742, Longitude = 16.0003, DayNumber = 3, OrderIndex = 0, Category = StopCategory.Attraction, DurationMinutes = 180, Notes = "Uma das maiores quedas de água de África." });
        itinerary.Stops.Add(new ItineraryStop { Name = "Fenda da Tundavala", Address = "Lubango, Huíla", Latitude = -14.8172, Longitude = 13.3828, DayNumber = 4, OrderIndex = 0, Category = StopCategory.Attraction, DurationMinutes = 120, Notes = "Miradouro natural no planalto da Huíla." });
        itinerary.Stops.Add(new ItineraryStop { Name = "Serra da Leba", Address = "Huíla", Latitude = -15.0719, Longitude = 13.2328, DayNumber = 5, OrderIndex = 0, Category = StopCategory.Attraction, DurationMinutes = 150, Notes = "Estrada panorâmica entre Lubango e Namibe." });

        itinerary.Attractions.Add(new ItineraryAttraction { PlaceId = "ao-fortaleza-sao-miguel", Name = "Fortaleza de São Miguel", Category = "Património", Latitude = -8.8167, Longitude = 13.2344, Rating = 4.5 });
        itinerary.Attractions.Add(new ItineraryAttraction { PlaceId = "ao-kalandula-falls", Name = "Quedas de Kalandula", Category = "Natureza", Latitude = -9.0742, Longitude = 16.0003, Rating = 4.8 });
        itinerary.Attractions.Add(new ItineraryAttraction { PlaceId = "ao-tundavala", Name = "Fenda da Tundavala", Category = "Miradouro", Latitude = -14.8172, Longitude = 13.3828, Rating = 4.7 });
        itinerary.Expenses.Add(new ItineraryExpense { Category = "Transporte", Description = "Voos domésticos e transfers", Amount = 260000, CurrencyCode = "AOA", Date = startDate });
        itinerary.Expenses.Add(new ItineraryExpense { Category = "Alojamento", Description = "Hotéis em Luanda, Malanje e Lubango", Amount = 330000, CurrencyCode = "AOA", Date = startDate.AddDays(1) });
        itinerary.Expenses.Add(new ItineraryExpense { Category = "Atividades", Description = "Guias locais e entradas", Amount = 95000, CurrencyCode = "AOA", Date = startDate.AddDays(2) });

        return itinerary;
    }
}
