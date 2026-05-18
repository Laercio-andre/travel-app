using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using TravelSystem.Application.DTOs.AI;
using TravelSystem.Application.Interfaces;
using TravelSystem.Domain.Entities;
using TravelSystem.Domain.Interfaces;

namespace TravelSystem.Application.Services;

public class AiAssistantService : IAiAssistantService
{
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;

    public AiAssistantService(IUnitOfWork uow, IConfiguration config)
    {
        _uow = uow;
        _config = config;
    }

    public async Task<AiChatResponse> ChatAsync(Guid userId, AiChatRequest request, CancellationToken ct = default)
    {
        var itinerary = await _uow.Itineraries.GetWithChatHistoryAsync(request.ItineraryId, ct)
            ?? throw new KeyNotFoundException("ITINERARY_NOT_FOUND");

        if (itinerary.UserId != userId)
            throw new UnauthorizedAccessException("ACCESS_DENIED");

        // Save user message
        var userMessage = new AiChatMessage
        {
            ItineraryId = request.ItineraryId,
            Role = "user",
            Content = request.Message
        };
        itinerary.ChatHistory.Add(userMessage);
        await _uow.CommitAsync(ct);

        // Build context for AI
        var systemPrompt = BuildSystemPrompt(itinerary, request.Language);
        var history = itinerary.ChatHistory
            .OrderBy(m => m.CreatedAt)
            .Select(m => new { role = m.Role, content = m.Content })
            .ToList();

        // Call AI API
        var aiContent = await CallAiApiAsync(systemPrompt, history, ct);

        // Save assistant reply
        var assistantMessage = new AiChatMessage
        {
            ItineraryId = request.ItineraryId,
            Role = "assistant",
            Content = aiContent
        };
        itinerary.ChatHistory.Add(assistantMessage);
        await _uow.CommitAsync(ct);

        return new AiChatResponse(assistantMessage.Id, assistantMessage.Content, assistantMessage.CreatedAt, assistantMessage.Role);
    }

    public async Task<string> SuggestItineraryAsync(Guid userId, AiSuggestRequest request, CancellationToken ct = default)
    {
        var lang = request.Language == "pt" ? "Português" : "English";
        var systemPrompt = $"Você é um especialista em viagens. Responda sempre em {lang}.";
        var userPrompt = $@"Crie um roteiro de viagem para {request.Destination} com {request.DurationDays} dias.
{(request.Interests != null ? $"Interesses: {request.Interests}" : "")}
{(request.Budget.HasValue ? $"Orçamento: {request.Budget:F2}" : "")}
Inclua atrações, restaurantes e dicas práticas organizadas por dia.";

        var messages = new[] { new { role = "user", content = userPrompt } }.ToList();
        return await CallAiApiAsync(systemPrompt, messages, ct);
    }

    public async Task<IEnumerable<AiChatResponse>> GetChatHistoryAsync(Guid itineraryId, Guid userId, CancellationToken ct = default)
    {
        var itinerary = await _uow.Itineraries.GetWithChatHistoryAsync(itineraryId, ct)
            ?? throw new KeyNotFoundException("ITINERARY_NOT_FOUND");

        if (itinerary.UserId != userId)
            throw new UnauthorizedAccessException("ACCESS_DENIED");

        return itinerary.ChatHistory
            .OrderBy(m => m.CreatedAt)
            .Select(m => new AiChatResponse(m.Id, m.Content, m.CreatedAt, m.Role));
    }

    public async Task ClearChatHistoryAsync(Guid itineraryId, Guid userId, CancellationToken ct = default)
    {
        var itinerary = await _uow.Itineraries.GetWithChatHistoryAsync(itineraryId, ct)
            ?? throw new KeyNotFoundException("ITINERARY_NOT_FOUND");

        if (itinerary.UserId != userId)
            throw new UnauthorizedAccessException("ACCESS_DENIED");

        itinerary.ChatHistory.Clear();
        await _uow.CommitAsync(ct);
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private static string BuildSystemPrompt(Itinerary itinerary, string language)
    {
        var lang = language == "pt" ? "Português Angolano" : "English";
        return $@"Você é um assistente de viagem inteligente e simpático. Responda sempre em {lang}.
Contexto da viagem atual:
- Destino: {itinerary.Destination}
- Período: {itinerary.StartDate:dd/MM/yyyy} a {itinerary.EndDate:dd/MM/yyyy} ({(itinerary.EndDate - itinerary.StartDate).Days + 1} dias)
{(itinerary.Budget.HasValue ? $"- Orçamento: {itinerary.CurrencyCode} {itinerary.Budget:F2}" : "")}
- Paragens planeadas: {itinerary.Stops.Count}

Ajude o viajante com sugestões de atrações, restaurantes, transporte, alojamento e dicas locais.
Seja conciso, prático e entusiasmado.";
    }

    private async Task<string> CallAiApiAsync(string systemPrompt, IEnumerable<object> messages, CancellationToken ct)
    {
        var apiKey = _config["AI:ApiKey"] ?? throw new InvalidOperationException("AI API key not configured");
        var apiUrl = _config["AI:ApiUrl"] ?? "https://api.anthropic.com/v1/messages";
        var model = _config["AI:Model"] ?? "claude-sonnet-4-20250514";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var payload = new
        {
            model,
            max_tokens = 1024,
            system = systemPrompt,
            messages
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(apiUrl, content, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Empty AI response");

        return result.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;
    }
}
