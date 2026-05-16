using System.Text;
using Amora.Api.Infrastructure;
using Amora.Api.Middleware;
using Amora.Application;
using Amora.Application.Abstractions;
using Amora.Application.Services;
using Amora.Api.Hubs;
using Amora.Domain.Interfaces;
using Amora.Infrastructure.Data;
using Amora.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using Amazon.S3;
using Amora.Application.Iap;
using Amora.Infrastructure.Iap;
using Amora.Infrastructure.Presence;
using Amora.Infrastructure.Scheduling;
using Amora.Infrastructure.Services;
using Amora.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(MongoDbOptions.SectionName));

builder.Services.AddDbContext<AmoraDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Postgres")
        ?? "Host=localhost;Port=5432;Database=AmoraCoreDb;Username=postgres;Password=your_password";

    options.UseNpgsql(connectionString);
});

builder.Services.AddSingleton<MongoDB.Driver.IMongoClient>(_ =>
{
    var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb")
        ?? "mongodb://localhost:27017";

    return new MongoDB.Driver.MongoClient(mongoConnectionString);
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IVoicePostRepository, VoicePostRepository>();
builder.Services.AddScoped<IVoiceCommentRepository, VoiceCommentRepository>();
builder.Services.AddScoped<IMatchConnectionRepository, MatchConnectionRepository>();
builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
builder.Services.AddScoped<IUserReportRepository, UserReportRepository>();
builder.Services.AddScoped<IUserBlockRepository, UserBlockRepository>();
builder.Services.AddScoped<IPetRepository, PetRepository>();
builder.Services.AddScoped<IShopRepository, ShopRepository>();
builder.Services.AddScoped<IPetTransactionRepository, PetTransactionRepository>();
builder.Services.AddScoped<IIapPurchaseRepository, IapPurchaseRepository>();

builder.Services.Configure<IapOptions>(builder.Configuration.GetSection(IapOptions.SectionName));
builder.Services.AddHttpClient("AppleIap");
builder.Services.AddHttpClient("GoogleIap");
builder.Services.AddScoped<AppleAppStorePurchaseVerifier>();
builder.Services.AddScoped<GooglePlayPurchaseVerifier>();
builder.Services.AddScoped<IInAppPurchaseVerifier, CompositeInAppPurchaseVerifier>();

builder.Services.AddSingleton<IMatchPresenceTracker, InMemoryMatchPresenceTracker>();

builder.Services.AddApplication();
builder.Services.AddAmoraQuartzJobs();

builder.Services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();
builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();
builder.Services.AddScoped<IPetRealtimeNotifier, SignalRPetRealtimeNotifier>();
builder.Services.AddScoped<VoicePostService>();
builder.Services.AddScoped<VoiceCommentService>();
builder.Services.AddScoped<MatchService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<TrustSafetyService>();
builder.Services.AddScoped<ProfileService>();

builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddScoped<IStorageService, S3StorageService>();

// Message Bus — Singleton vì connection RabbitMQ được tái sử dụng
builder.Services.AddSingleton<IMessageBus>(_ =>
{
    var rabbitUrl = builder.Configuration["RabbitMQ:Url"] ?? "amqp://guest:guest@localhost:5672//";
    return RabbitMqMessageBus.CreateAsync(rabbitUrl).GetAwaiter().GetResult();
});
builder.Services.AddSingleton<IMessagePublisher>(_ =>
{
    var rabbitUrl = builder.Configuration["RabbitMQ:Url"] ?? "amqp://guest:guest@localhost:5672//";
    return RabbitMqMessagePublisher.CreateAsync(rabbitUrl).GetAwaiter().GetResult();
});
builder.Services.AddScoped<AudioProcessingService>();

// Handshake 24h — Background job tự động expire match không có tin nhắn
builder.Services.AddHostedService<Amora.Infrastructure.Services.HandshakeExpiryService>();
builder.Services.AddHostedService<Amora.Infrastructure.Messaging.VibeResultConsumerService>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-only-secret-key-change-me-please-use-a-longer-256-bit-key";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            NameClaimType = "name",
            RoleClaimType = "role"
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrWhiteSpace(accessToken) &&
                    (context.HttpContext.Request.Path.StartsWithSegments("/hubs/chat")
                     || context.HttpContext.Request.Path.StartsWithSegments("/hubs/pet")))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("comment", context =>
    {
        var userId = context.User.FindFirst("id")?.Value ?? "anonymous";
        var postId = context.Request.RouteValues.TryGetValue("postId", out var value) ? value?.ToString() ?? "unknown" : "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"comment:{userId}:{postId}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 1,
                Window = TimeSpan.FromDays(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    options.AddPolicy("message", context =>
    {
        var userId = context.User.FindFirst("id")?.Value ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"message:{userId}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers().RequireAuthorization();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<PetHub>("/hubs/pet");

app.Run();
