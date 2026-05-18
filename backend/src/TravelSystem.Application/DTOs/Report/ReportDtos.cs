namespace TravelSystem.Application.DTOs.Report;

public record ReportRequest(
    Guid? ItineraryId,
    DateTime? From,
    DateTime? To,
    string Format = "pdf",
    string Language = "pt"
);

public record ExpenseReportDto(
    Guid ItineraryId,
    string ItineraryTitle,
    string Destination,
    DateTime StartDate,
    DateTime EndDate,
    decimal Budget,
    decimal TotalSpent,
    decimal Remaining,
    List<ExpenseCategoryDto> ByCategory,
    List<ReportExpenseDto>? Expenses = null
)
{
    public decimal TotalBudget => Budget;
    public decimal Balance => Remaining;
}

public record ExpenseCategoryDto(
    string Category,
    decimal Total,
    int Count,
    double Percentage
);

public record ReportExpenseDto(
    Guid Id,
    string Category,
    string Description,
    decimal Amount,
    DateTime Date
);
