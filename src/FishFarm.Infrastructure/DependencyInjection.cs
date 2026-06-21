using CloudinaryDotNet;
using FishFarm.Application.Common.Interfaces;
using FishFarm.Domain.Interfaces;
using FishFarm.Infrastructure.Persistence;
using FishFarm.Infrastructure.Persistence.Interceptors;
using FishFarm.Infrastructure.Persistence.Repositories;
using FishFarm.Infrastructure.Services;
using FishFarm.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FishFarm.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── EF Core Interceptor ───────────────────────────────────────────────
        services.AddSingleton<AuditAndSoftDeleteInterceptor>();

        // ── EF Core DbContext ─────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options
                .UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null);
                    })
                .AddInterceptors(sp.GetRequiredService<AuditAndSoftDeleteInterceptor>());
        });

        // ── Repositories ──────────────────────────────────────────────────────
        services.AddScoped<IFishFarmRepository, FishFarmRepository>();
        services.AddScoped<IWorkerRepository, WorkerRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── Cloudinary ────────────────────────────────────────────────────────
        var cloudinarySettings = configuration
            .GetSection(CloudinarySettings.SectionName)
            .Get<CloudinarySettings>()
            ?? throw new InvalidOperationException("Cloudinary settings are not configured.");

        var account = new Account(
            cloudinarySettings.CloudName,
            cloudinarySettings.ApiKey,
            cloudinarySettings.ApiSecret);

        services.AddSingleton(new Cloudinary(account) { Api = { Secure = true } });
        services.AddScoped<ICloudinaryService, CloudinaryService>();

        return services;
    }
}
