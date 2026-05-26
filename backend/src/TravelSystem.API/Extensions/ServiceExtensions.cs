using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TravelSystem.API.Middleware;
using TravelSystem.Application.Interfaces;
using TravelSystem.Application.Services;
using TravelSystem.Domain.Entities;
using TravelSystem.Domain.Interfaces;
using TravelSystem.Infrastructure.Data;
using TravelSystem.Infrastructure.Data.Repositories;
using TravelSystem.Infrastructure.Services;

namespace TravelSystem.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseMySql(
                config.GetConnectionString("Default"),
                ServerVersion.AutoDetect(config.GetConnectionString("Default")),
                sql => sql.EnableRetryOnFailure(3)
            )
        );
        return services;
    }

    public static IServiceCollection AddIdentityConfig(this IServiceCollection services)
    {
        services.AddIdentity<User, IdentityRole<Guid>>(opts =>
        {
            opts.Password.RequireDigit = true;
            opts.Password.RequiredLength = 6;
            opts.Password.RequireUppercase = false;
            opts.Password.RequireNonAlphanumeric = false;
            opts.User.RequireUniqueEmail = true;
            opts.Lockout.MaxFailedAccessAttempts = 5;
            opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var key = Encoding.UTF8.GetBytes(config["Jwt:Secret"]!);

        services.AddAuthentication(opts =>
        {
            opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(opts =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = config["Jwt:Issuer"],
                ValidAudience = config["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            opts.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = ctx =>
                {
                    if (ctx.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        ctx.Response.Headers.Append("Token-Expired", "true");
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(opts =>
        {
            opts.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "TravelSystem API",
                Version = "v1",
                Description = "API para o sistema de organização de viagens"
            });

            opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Insira o token JWT: Bearer {token}"
            });

            opts.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    []
                }
            });
        });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Infrastructure
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Application Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IItineraryService, ItineraryService>();
        services.AddScoped<IHotelService, HotelService>();
        services.AddScoped<IFlightService, FlightService>();
        services.AddScoped<IAiAssistantService, AiAssistantService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }

    public static IServiceCollection AddLocalizationConfig(this IServiceCollection services)
    {
        services.AddLocalization(opts => opts.ResourcesPath = "Resources");
        services.Configure<RequestLocalizationOptions>(opts =>
        {
            var supported = new[] { "pt", "en" };
            opts.SetDefaultCulture("pt");
            opts.AddSupportedCultures(supported);
            opts.AddSupportedUICultures(supported);
        });
        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration config)
    {
        services.AddCors(opts => opts.AddPolicy("AllowAngular", policy =>
            policy.WithOrigins(config["Frontend:Url"] ?? "http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()
        ));
        return services;
    }
}
