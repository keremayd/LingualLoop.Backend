using Common.Options;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LingualLoop.Hangfire.Extensions;


public static class HangfireExtensions
{
    public static IServiceCollection AddHangfire(this IServiceCollection services, IConfiguration configuration)
    {
        var hangfireOptions = configuration.GetSection(nameof(HangfireOptions)).Get<HangfireOptions>()
                              ?? throw new Exception(
                                  "ERROR : Hangfire options is null. Please check appsettings.json!");

        return services.AddHangfire(config =>
            {
                config.UsePostgreSqlStorage(
                    hangfireOptions?.ConnectionString,
                    new PostgreSqlStorageOptions
                    {
                        InvisibilityTimeout = TimeSpan.FromHours(hangfireOptions.TimeSpanFromHours),
                        SchemaName = hangfireOptions.SchemaName,
                        DistributedLockTimeout = TimeSpan.FromMinutes(10)
                    });
            })
            .AddHangfireServer();
    }


}
