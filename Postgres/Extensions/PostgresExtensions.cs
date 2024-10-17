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

        if (postgresOptions == null || string.IsNullOrEmpty(postgresOptions?.LingualLoopConnectionString))
            throw new Exception("LingualLoop options cannot be null or empty");

        return services
            .AddRepositories()
            .AddEntityFrameworkNpgsql()
            .AddDbContext<LingualLoopContext>(options =>
                options.UseNpgsql(postgresOptions.LingualLoopConnectionString, b => b.MigrationsAssembly("LingualLoop.Api")));
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        return services
            .AddScoped(typeof(ILingualLoopGenericRepository<>), typeof(LingualLoopGenericRepository<>));
    }
}