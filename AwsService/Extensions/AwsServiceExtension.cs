using Amazon.S3;
using AwsService.Abstractions;
using AwsService.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace AwsService.Extensions;

public static class AwsServiceExtension
{
    public static IServiceCollection AddAwsS3Service(this IServiceCollection services, IConfiguration configuration)
    {
        var configurationSection = configuration.GetSection("AwsOptions");
        services.Configure<AwsOptions>(configurationSection);
        
        var options = configurationSection.Get<AwsOptions>()!;
        
        services.AddScoped<IAmazonS3>(provider => new AmazonS3Client(
            options.AccessKey,
            options.SecretKey,
            Amazon.RegionEndpoint.GetBySystemName(options.Region)
        ));

        services.AddScoped<IAwsService, Services.AwsService>();
        services.AddScoped<IAwsBucketNameFactory, AwsBucketNameFactory>();
        
        return services;
    }
}