using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Service.Handlers.Commands;

namespace Service.Extensions;

public static class ServiceExtensions
{
    // public static IServiceCollection AddHandlers(this IServiceCollection services){
    //    return services.AddMediatR(cfg=>cfg.RegisterServicesFromAssemblies(typeof(GetUserByIdQueryHandler).Assembly));
    //
    // }
}