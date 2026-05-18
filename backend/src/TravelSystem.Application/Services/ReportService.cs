using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using iTextSharp.text;
using iTextSharp.text.pdf;
using TravelSystem.Application.DTOs.Report;
using TravelSystem.Application.Interfaces;
using TravelSystem.Domain.Interfaces;

namespace TravelSystem.Application.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _uow;

    public ReportService(IUnitOfWork uow) => _uow = uow;

    public async Task<ExpenseReportDto> GetExpenseSummaryAsync(Guid itineraryId, Guid userId, CancellationToken ct = default)
    {
        var itinerary = await _uow.Itineraries.GetFullItineraryAsync(itineraryId, ct)
            ?? throw new KeyNotFoundException("ITINERARY_NOT_FOUND");

        if (itinerary.UserId != userId)
            throw new UnauthorizedAccessException("ACCESS_DENIED");

        var totalSpent = itinerary.Expenses.Sum(e => e.Amount);
        var budget = itinerary.Budget ?? 0;

        var byCategory = itinerary.Expenses
            .GroupBy(e => e.Category)
            .Select(g => new ExpenseCategoryDto(
                g.Key,
                g.Sum(e => e.Amount),
                g.Count(),
                totalSpent > 0 ? (double)(g.Sum(e => e.Amount) / totalSpent * 100) : 0
            ))
            .OrderByDescending(c => c.Total)
            .ToList();

        var expenses = itinerary.Expenses
            .OrderByDescending(e => e.Date)
            .Select(e => new ReportExpenseDto(e.Id, e.Category, e.Description, e.Amount, e.Date))
            .ToList();

        return new ExpenseReportDto(
            itinerary.Id, itinerary.Title, itinerary.Destination,
            itinerary.StartDate, itinerary.EndDate,
            budget, totalSpent, budget - totalSpent, byCategory, expenses
        );
    }

    public async Task<byte[]> GenerateCsvReportAsync(Guid userId, ReportRequest request, CancellationToken ct = default)
    {
        var itineraries = request.ItineraryId.HasValue
            ? [await _uow.Itineraries.GetFullItineraryAsync(request.ItineraryId.Value, ct)
                ?? throw new KeyNotFoundException("ITINERARY_NOT_FOUND")]
            : (await _uow.Itineraries.GetByUserIdAsync(userId, ct)).ToArray();

        var records = itineraries
            .SelectMany(i => i.Expenses.Select(e => new
            {
                Itinerary = i.Title,
                Destination = i.Destination,
                Date = e.Date.ToString("yyyy-MM-dd"),
                Category = e.Category,
                Description = e.Description,
                Amount = e.Amount,
                Currency = e.CurrencyCode
            }))
            .OrderBy(r => r.Date)
            .ToList();

        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, new UTF8Encoding(true));
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            HasHeaderRecord = true
        };
        using var csv = new CsvWriter(writer, config);
        await csv.WriteRecordsAsync(records, ct);
        await writer.FlushAsync(ct);
        return ms.ToArray();
    }

    public async Task<byte[]> GeneratePdfReportAsync(Guid userId, ReportRequest request, CancellationToken ct = default)
    {
        var itinerary = request.ItineraryId.HasValue
            ? await _uow.Itineraries.GetFullItineraryAsync(request.ItineraryId.Value, ct)
                ?? throw new KeyNotFoundException("ITINERARY_NOT_FOUND")
            : throw new ArgumentException("ITINERARY_ID_REQUIRED");

        if (itinerary.UserId != userId)
            throw new UnauthorizedAccessException("ACCESS_DENIED");

        using var ms = new MemoryStream();
        var doc = new Document(PageSize.A4, 36, 36, 54, 36);
        PdfWriter.GetInstance(doc, ms);
        doc.Open();

        // Title
        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20, new BaseColor(0, 0, 0));
        var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, new BaseColor(64, 64, 64));
        var bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, new BaseColor(0, 0, 0));
        var mutedFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, new BaseColor(100, 100, 100));

        doc.Add(new Paragraph("TravelSystem — Relatório de Viagem", titleFont) { SpacingAfter = 6 });
        doc.Add(new Paragraph($"{itinerary.Destination}  •  {itinerary.StartDate:dd/MM/yyyy} – {itinerary.EndDate:dd/MM/yyyy}", mutedFont) { SpacingAfter = 20 });

        // Summary table
        doc.Add(new Paragraph("Resumo Financeiro", headerFont) { SpacingAfter = 8 });
        var summaryTable = new PdfPTable(2) { WidthPercentage = 60 };
        summaryTable.SetWidths([50f, 50f]);

        var totalSpent = itinerary.Expenses.Sum(e => e.Amount);
        var budget = itinerary.Budget ?? 0;

        AddTableRow(summaryTable, "Orçamento:", $"{itinerary.CurrencyCode} {budget:N2}", headerFont, bodyFont);
        AddTableRow(summaryTable, "Total Gasto:", $"{itinerary.CurrencyCode} {totalSpent:N2}", headerFont, bodyFont);
        AddTableRow(summaryTable, "Saldo:", $"{itinerary.CurrencyCode} {(budget - totalSpent):N2}", headerFont, bodyFont);
        doc.Add(summaryTable);
        doc.Add(new Paragraph(" ") { SpacingAfter = 12 });

        // Expenses by category
        doc.Add(new Paragraph("Despesas por Categoria", headerFont) { SpacingAfter = 8 });
        var catTable = new PdfPTable(3) { WidthPercentage = 80 };
        catTable.SetWidths([50f, 25f, 25f]);
        AddTableHeader(catTable, ["Categoria", "Total", "%"], headerFont);

        var byCategory = itinerary.Expenses
            .GroupBy(e => e.Category)
            .Select(g => (g.Key, g.Sum(e => e.Amount)))
            .OrderByDescending(c => c.Item2);

        foreach (var (cat, total) in byCategory)
        {
            var pct = totalSpent > 0 ? (double)(total / totalSpent * 100) : 0;
            catTable.AddCell(new PdfPCell(new Phrase(cat, bodyFont)) { Border = 0, PaddingBottom = 4 });
            catTable.AddCell(new PdfPCell(new Phrase($"{total:N2}", bodyFont)) { Border = 0, HorizontalAlignment = Element.ALIGN_RIGHT });
            catTable.AddCell(new PdfPCell(new Phrase($"{pct:F1}%", mutedFont)) { Border = 0, HorizontalAlignment = Element.ALIGN_RIGHT });
        }
        doc.Add(catTable);
        doc.Add(new Paragraph(" ") { SpacingAfter = 12 });

        // Itinerary stops
        if (itinerary.Stops.Any())
        {
            doc.Add(new Paragraph("Roteiro Detalhado", headerFont) { SpacingAfter = 8 });
            foreach (var dayGroup in itinerary.Stops.GroupBy(s => s.DayNumber).OrderBy(g => g.Key))
            {
                doc.Add(new Paragraph($"Dia {dayGroup.Key}", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)) { SpacingAfter = 4 });
                foreach (var stop in dayGroup.OrderBy(s => s.OrderIndex))
                {
                    doc.Add(new Paragraph($"  • {stop.Name}{(stop.Address != null ? $" — {stop.Address}" : "")}", bodyFont));
                }
                doc.Add(new Paragraph(" "));
            }
        }

        doc.Add(new Paragraph($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}", mutedFont));
        doc.Close();
        return ms.ToArray();
    }

    private static void AddTableRow(PdfPTable table, string label, string value, Font labelFont, Font valueFont)
    {
        table.AddCell(new PdfPCell(new Phrase(label, labelFont)) { Border = 0, PaddingBottom = 4 });
        table.AddCell(new PdfPCell(new Phrase(value, valueFont)) { Border = 0, HorizontalAlignment = Element.ALIGN_RIGHT });
    }

    private static void AddTableHeader(PdfPTable table, string[] headers, Font font)
    {
        foreach (var header in headers)
            table.AddCell(new PdfPCell(new Phrase(header, font)) { BackgroundColor = new BaseColor(240, 240, 240), PaddingBottom = 6 });
    }
}
