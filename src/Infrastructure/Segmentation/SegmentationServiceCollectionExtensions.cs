using Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Segmentation;

public static class SegmentationServiceCollectionExtensions
{
    public static IServiceCollection AddSegmentationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SegmentationOptions>(configuration.GetSection(SegmentationOptions.SectionName));
        services.PostConfigure<SegmentationOptions>(options =>
        {
            options.ConnectionString ??= configuration.GetConnectionString("SegmentationConnection")
                ?? configuration.GetConnectionString("DefaultConnection");
        });

        services.AddScoped<ISegmentationService, SegmentationService>();

        services.AddSingleton<IValidateOptions<SegmentationOptions>, SegmentationOptionsValidation>();

        return services;
    }
}
