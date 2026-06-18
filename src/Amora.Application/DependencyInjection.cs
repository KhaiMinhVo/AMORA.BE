using System.Reflection;
using Amora.Application.Pets;
using Amora.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Amora.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddScoped<PetCoordinator>();
        services.AddScoped<PetShopService>();
        services.AddScoped<PetFeatureGateService>();
        services.AddScoped<Iap.IapGemService>();
        services.AddScoped<Iap.IapWebhookService>();
        services.AddScoped<Iap.DiamondRewardService>();
        services.AddScoped<AuthService>();
        services.AddScoped<SubscriptionService>();
        services.AddScoped<PostPromotionService>();

        services.AddHttpClient<AiModerationService>();
        return services;
    }
}
