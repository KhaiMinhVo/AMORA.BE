using System.Reflection;
using Amora.Application.Pets;
using Microsoft.Extensions.DependencyInjection;

namespace Amora.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddScoped<PetCoordinator>();
        services.AddScoped<PetShopService>();
        services.AddScoped<Iap.IapGemService>();
        return services;
    }
}
