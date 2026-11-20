using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Postgres.Abstractions;
using Postgres.Models;
using Postgres.Options;

namespace Postgres.Extensions;

public static class PostgresExtensions
{
    public static IServiceCollection AddPostgres(this IServiceCollection services, IConfiguration configuration)
    {
        var configurationSection = configuration.GetSection("PostgresOptions");
        services.Configure<PostgresOptions>(configurationSection);
        PostgresOptions? postgresOptions = configurationSection.Get<PostgresOptions>();

        if (postgresOptions == null || string.IsNullOrEmpty(postgresOptions?.StelindaConnectionString))
            throw new Exception("Stelinda options cannot be null or empty");

        return services
            .AddRepositories()
            .AddEntityFrameworkNpgsql()
            .AddDbContext<StelindaContext>(options =>
                options.UseNpgsql(postgresOptions.StelindaConnectionString, b => b.MigrationsAssembly("Stelinda.Api")));
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        return services
            .AddScoped(typeof(IStelindaGenericRepository<>), typeof(StelindaGenericRepository<>));
    }
}