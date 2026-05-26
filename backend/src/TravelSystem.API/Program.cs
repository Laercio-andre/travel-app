using FluentValidation;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TravelSystem.API.Extensions;
using TravelSystem.API.Middleware;
using TravelSystem.Application.Validators;
using TravelSystem.Infrastructure.Data;
using TravelSystem.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ──────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/travelsystem-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ── Services ─────────────────────────────────────────────────────────────────
builder.Services
    .AddDatabase(builder.Configuration)
    .AddIdentityConfig()
    .AddJwtAuthentication(builder.Configuration)
    .AddApplicationServices()
    .AddLocalizationConfig()
    .AddCorsPolicy(builder.Configuration)
    .AddSwagger();

// Validators (FluentValidation)
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(TravelSystem.Application.Mappings.MappingProfile));

// Background job — flight price alert checker
builder.Services.AddHostedService<FlightAlertBackgroundService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── App Pipeline ─────────────────────────────────────────────────────────────
var app = builder.Build();

// Run migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();

    if (!await TableExistsAsync(db, "Roles"))
    {
        if (!app.Environment.IsDevelopment())
        {
            throw new InvalidOperationException("Database schema is incomplete. Apply migrations or recreate the database before starting the API.");
        }

        Log.Warning("Development database schema is incomplete. Recreating database...");
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(opts =>
    {
        opts.SwaggerEndpoint("/swagger/v1/swagger.json", "TravelSystem API v1");
        opts.RoutePrefix = "swagger";
    });
}

app.UseMiddleware<ExceptionHandlerMiddleware>();
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("pt"),
    SupportedCultures = [new("pt"), new("en")],
    SupportedUICultures = [new("pt"), new("en")]
});

app.UseSerilogRequestLogging();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

static async Task<bool> TableExistsAsync(AppDbContext db, string tableName)
{
    var connection = db.Database.GetDbConnection();
    if (connection.State != System.Data.ConnectionState.Open)
    {
        await connection.OpenAsync();
    }

    await using var command = connection.CreateCommand();
    command.CommandText = """
        SELECT COUNT(*)
        FROM information_schema.tables
        WHERE table_schema = DATABASE()
          AND table_name = @tableName
        """;

    var parameter = command.CreateParameter();
    parameter.ParameterName = "@tableName";
    parameter.Value = tableName;
    command.Parameters.Add(parameter);

    var result = await command.ExecuteScalarAsync();
    return Convert.ToInt32(result) > 0;
}
