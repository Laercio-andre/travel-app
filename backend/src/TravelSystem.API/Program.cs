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
    await db.Database.MigrateAsync();
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
app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
