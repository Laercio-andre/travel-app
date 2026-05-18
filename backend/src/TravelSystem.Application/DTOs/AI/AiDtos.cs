namespace TravelSystem.Application.DTOs.AI;

public record AiChatRequest(
    Guid ItineraryId,
    string Message,
    string Language = "pt"
);

public record AiChatResponse(
    Guid MessageId,
    string Content,
    DateTime CreatedAt,
    string Role = "assistant"
)
{
    public Guid Id => MessageId;
}

public record AiSuggestRequest(
    string Destination,
    int DurationDays,
    string? Interests,
    decimal? Budget,
    string Language = "pt"
);
