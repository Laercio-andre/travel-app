namespace TravelSystem.Domain.Enums;

public enum ItineraryStatus
{
    Draft = 0,
    Active = 1,
    Completed = 2,
    Cancelled = 3
}

public enum StopCategory
{
    Attraction = 0,
    Restaurant = 1,
    Hotel = 2,
    Transport = 3,
    Activity = 4,
    Shopping = 5,
    Other = 99
}

public enum BookingType
{
    Hotel = 0,
    Flight = 1
}

public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1,
    Cancelled = 2,
    Completed = 3,
    Failed = 4
}

public enum UserRole
{
    Traveler = 0,
    PremiumTraveler = 1,
    Admin = 2
}
