using System.Text;
using Amazon.Runtime;
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
using Amora.Infrastructure.HealthChecks;
using Amora.Infrastructure.Scheduling;
using Amora.Infrastructure.Services;
using Amora.Infrastructure.Messaging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog: replace default logging with structured output ─────────
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId();
});

// ── OpenTelemetry: traces + metrics ─────────────────────────────────
builder.AddAmoraObservability();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://amora-fe.vercel.app", "https://amora.pro.vn")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

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
builder.Services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
builder.Services.AddScoped<IIapPurchaseRepository, IapPurchaseRepository>();
builder.Services.AddScoped<IChatReadStateRepository, ChatReadStateRepository>();
builder.Services.AddScoped<IMatchMediaUsageRepository, MatchMediaUsageRepository>();
builder.Services.AddScoped<IIapWebhookEventRepository, IapWebhookEventRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<Amora.Application.Payment.VnPayConfig>(builder.Configuration.GetSection("VnPay"));
builder.Services.Configure<Amora.Application.Payment.PayOs.PayOsConfig>(builder.Configuration.GetSection("PayOS"));
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services.Configure<IapOptions>(builder.Configuration.GetSection(IapOptions.SectionName));
builder.Services.AddHttpClient("AppleIap");
builder.Services.AddHttpClient("GoogleIap");
builder.Services.AddScoped<AppleAppStorePurchaseVerifier>();
builder.Services.AddScoped<GooglePlayPurchaseVerifier>();
builder.Services.AddScoped<IInAppPurchaseVerifier, CompositeInAppPurchaseVerifier>();
builder.Services.AddSingleton<AppleServerNotificationVerifier>();
builder.Services.AddSingleton<GoogleWebhookTokenValidator>();

builder.Services.AddPresenceTracking(builder.Configuration);

builder.Services.AddApplication();
builder.Services.AddAmoraQuartzJobs();

builder.Services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();
builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();
builder.Services.AddScoped<IPetRealtimeNotifier, SignalRPetRealtimeNotifier>();
builder.Services.AddScoped<Amora.Application.Payment.PayOs.PayOsService>();
builder.Services.AddScoped<VoicePostService>();
builder.Services.AddScoped<VoiceCommentService>();
builder.Services.AddScoped<MatchService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<TrustSafetyService>();
builder.Services.AddScoped<AdminModerationService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<NotificationService>();

var awsOptions = builder.Configuration.GetAWSOptions();
var awsServiceUrl = builder.Configuration["AWS:ServiceURL"];
Amazon.AWSConfigsS3.UseSignatureVersion4 = true;
var s3Config = new AmazonS3Config();
s3Config.AuthenticationRegion = !string.IsNullOrWhiteSpace(awsServiceUrl) 
    ? "us-east-1" 
    : (awsOptions.Region?.SystemName ?? "ap-southeast-1");

if (!string.IsNullOrWhiteSpace(awsServiceUrl))
    s3Config.ServiceURL = awsServiceUrl;

if (builder.Configuration.GetValue<bool?>("AWS:ForcePathStyle") == true)
    s3Config.ForcePathStyle = true;

if (awsServiceUrl?.StartsWith("http://") == true)
    s3Config.UseHttp = true;

if (awsOptions.Region is not null && string.IsNullOrWhiteSpace(awsServiceUrl))
    s3Config.RegionEndpoint = awsOptions.Region;

builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddSingleton<IAmazonS3>(_ =>
{
    var accessKey = builder.Configuration["AWS:AccessKey"];
    var secretKey = builder.Configuration["AWS:SecretKey"];

    if (!string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey))
    {
        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        return new AmazonS3Client(credentials, s3Config);
    }

    return awsOptions.Credentials is not null
        ? new AmazonS3Client(awsOptions.Credentials, s3Config)
        : new AmazonS3Client(s3Config);
});
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IStorageService, S3StorageService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

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

builder.Services.AddHostedService<Amora.Infrastructure.Messaging.VibeResultConsumerService>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-only-secret-key-change-me-please-use-a-longer-256-bit-key";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "Amora",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "Amora",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
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

var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? builder.Configuration["Redis:ConnectionString"];

var healthChecks = builder.Services.AddHealthChecks()
    .AddDbContextCheck<AmoraDbContext>()
    .AddCheck<RabbitMqHealthCheck>("rabbitmq");

if (!string.IsNullOrWhiteSpace(redisConnectionString))
    healthChecks.AddCheck<RedisHealthCheck>("redis");

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

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AmoraDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("UserId", httpContext.User.FindFirst("id")?.Value ?? "anonymous");
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
    };
});

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowSpecificOrigins");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseMiddleware<BanCheckMiddleware>();

app.MapHealthChecks("/health");
app.MapAmoraObservability();
app.MapControllers().RequireAuthorization();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<PetHub>("/hubs/pet");
app.MapHub<CallHub>("/hubs/call");

app.Run();
