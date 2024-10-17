using System.Reflection;
using System.Text;
using Common.Options;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Postgres;
using Postgres.Models;
using Service.Handlers.Commands;

namespace Service.Extensions;

public static class ServiceExtensions
{
    public static void AddIdentity(this IServiceCollection services)
    {
        var builder = services.AddIdentity<User, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<LingualLoopContext>()
            .AddDefaultTokenProviders();
    }

    public static void AddJwt(this IServiceCollection services, IConfiguration configuration)
    {
        var configurationSection = configuration.GetSection("JwtOptions");
        JwtOptions? jwtOptions = configurationSection.Get<JwtOptions>();
        
        if (jwtOptions == null || string.IsNullOrEmpty(jwtOptions?.ValidAudience))
            throw new Exception("JwtOptions options cannot be null or empty");

        var secretKey = jwtOptions.SecretKey;
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true, //Expire süresini doğrula
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.ValidIssuer,
            ValidAudience = jwtOptions.ValidAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        });
    }
}