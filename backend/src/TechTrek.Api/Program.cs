using System.Text;
using StackExchange.Redis;
using Asp.Versioning;
using AspNetCoreRateLimit;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using TechTrek.Api.Middleware;
using TechTrek.Application.Common.Behaviors;
using TechTrek.Domain.Interfaces;
using TechTrek.Infrastructure.BackgroundServices;
using TechTrek.Persistence;
using TechTrek.Persistence.Repositories;
using TechTrek.Realtime.Hubs;
using TechTrek.Security.Interfaces;
using TechTrek.Security.Services;
using TechTrek.Shared.Constants;

// ============================================================
// SERILOG CONFIGURATION
// ============================================================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/techtrek-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("TechTrek API starting...");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ============================================================
    // CONFIGURATION
    // ============================================================
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
    builder.Services.Configure<EncryptionSettings>(builder.Configuration.GetSection("Encryption"));

    // ============================================================
    // DATABASE (PostgreSQL + EF Core 8)
    // ============================================================
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            npgsql => npgsql.MigrationsAssembly("TechTrek.Persistence")
        );

        if (builder.Environment.IsDevelopment())
            options.EnableSensitiveDataLogging().EnableDetailedErrors();
    });

    // ============================================================
    // REPOSITORIES (aggregate-focused, NOT generic)
    // ============================================================
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    builder.Services.AddScoped<IEmailTokenRepository, EmailTokenRepository>();
    builder.Services.AddScoped<ITeamRepository, TeamRepository>();
    builder.Services.AddScoped<IEventRepository, EventRepository>();
    builder.Services.AddScoped<ILeaderboardRepository, LeaderboardRepository>();
    builder.Services.AddScoped<IGameSessionRepository, GameSessionRepository>();
    builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
    builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
    builder.Services.AddScoped<IFeatureFlagRepository, FeatureFlagRepository>();
    builder.Services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
    builder.Services.AddScoped<IVolunteerAssignmentRepository, VolunteerAssignmentRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    // ============================================================
    // SECURITY SERVICES
    // ============================================================
    builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
    builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
    builder.Services.AddSingleton<ITokenService, TokenService>();

    // ============================================================
    // MEDIATR + PIPELINE BEHAVIORS
    // ============================================================
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(TechTrek.Application.Auth.Commands.RegisterCommand).Assembly);
    });

    builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

    // FluentValidation auto-discovery
    builder.Services.AddValidatorsFromAssembly(typeof(TechTrek.Application.Auth.Commands.RegisterCommand).Assembly);

    // ============================================================
    // JWT AUTHENTICATION
    // ============================================================
    var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // SignalR: get token from query string (websocket connections)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/hubs/game") ||
                     path.StartsWithSegments("/hubs/leaderboard") ||
                     path.StartsWithSegments("/hubs/admin")))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

    // ============================================================
    // AUTHORIZATION POLICIES
    // ============================================================
    builder.Services.AddAuthorizationBuilder()
        .AddPolicy(AuthorizationPolicies.ParticipantOnly, p =>
            p.RequireRole("Participant"))
        .AddPolicy(AuthorizationPolicies.VolunteerOnly, p =>
            p.RequireRole("Volunteer"))
        .AddPolicy(AuthorizationPolicies.AdminOnly, p =>
            p.RequireRole("Admin", "SuperAdmin"))
        .AddPolicy(AuthorizationPolicies.SuperAdminOnly, p =>
            p.RequireRole("SuperAdmin"))
        .AddPolicy(AuthorizationPolicies.StaffOrAbove, p =>
            p.RequireRole("Volunteer", "Organizer", "Admin", "SuperAdmin"));

    // ============================================================
    // SIGNALR + REDIS BACKPLANE
    // ============================================================
    var signalRBuilder = builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    });

    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
    if (!string.IsNullOrEmpty(redisConnectionString))
    {
        signalRBuilder.AddStackExchangeRedis(redisConnectionString, options =>
        {
            options.Configuration.ChannelPrefix = RedisChannel.Literal("TechTrek");
        });
        Log.Information("SignalR Redis backplane configured.");
    }
    else
    {
        Log.Warning("No Redis connection string found. SignalR running in-memory (not suitable for production).");
    }

    // ============================================================
    // RATE LIMITING (aspnetcoreratelimit)
    // ============================================================
    builder.Services.AddMemoryCache();
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

    // ============================================================
    // CORS
    // ============================================================
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("TechTrekFrontend", policy =>
        {
            var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:5173", "http://localhost:3000" };

            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Required for SignalR + HttpOnly cookies
        });
    });

    // ============================================================
    // API VERSIONING + SWAGGER
    // ============================================================
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "TechTrek Enterprise API",
            Version = "v1",
            Description = "Backend for TechTrek University Event Platform",
        });

        // JWT auth in Swagger UI
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter: Bearer {your JWT token}"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    // ============================================================
    // OPENTELEMETRY (Tracing)
    // ============================================================
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("TechTrek-API"))
        .WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation();
            // Add Jaeger or OTLP exporter if configured
        });

    // ============================================================
    // BACKGROUND SERVICES
    // ============================================================
    builder.Services.AddHostedService<OutboxDispatcher>();

    builder.Services.AddControllers();

    // ============================================================
    // BUILD + CONFIGURE PIPELINE
    // ============================================================
    var app = builder.Build();

    // Auto-migrate database on startup (dev only; use migration runner in prod)
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        // Seed initial data so the frontend has something to display
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var encryption = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
        await TechTrek.Persistence.Seed.DatabaseSeeder.SeedAsync(db, hasher, encryption);
    }

    // Order matters: exception handler must be first
    app.UseMiddleware<ExceptionHandlerMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TechTrek API v1"));
    }

    app.UseIpRateLimiting();
    app.UseHttpsRedirection();
    app.UseCors("TechTrekFrontend");
    app.UseAuthentication();
    app.UseAuthorization();

    // Map controllers
    app.MapControllers();

    // Map SignalR hubs
    app.MapHub<GameHub>("/hubs/game");
    app.MapHub<LeaderboardHub>("/hubs/leaderboard");
    app.MapHub<AdminHub>("/hubs/admin");

    // Health check endpoint
    app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

    Log.Information("TechTrek API configured. Starting...");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "TechTrek API failed to start.");
}
finally
{
    await Log.CloseAndFlushAsync();
}
