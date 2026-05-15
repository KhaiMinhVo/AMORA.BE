using System.Text;
using Amora.Api.Hubs;
using Amora.Api.Infrastructure;
using Amora.Api.Middleware;
using Amora.Application.Abstractions;
using Amora.Application.Services;
using Amora.Domain.Interfaces;
using Amora.Infrastructure.Data;
using Amora.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(MongoDbOptions.SectionName));

builder.Services.AddDbContext<AmoraDbContext>(options =>
{
    var connectionString = "Host=127.0.0.1;Port=5444;Database=AmoraCoreDb;Username=postgres;Password=postgres";

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

builder.Services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();
builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();
builder.Services.AddScoped<VoicePostService>();
builder.Services.AddScoped<VoiceCommentService>();
builder.Services.AddScoped<MatchService>();
builder.Services.AddScoped<ChatService>();

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
                if (!string.IsNullOrWhiteSpace(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs/chat"))
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

app.Run();
