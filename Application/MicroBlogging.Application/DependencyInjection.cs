using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using MicroBlogging.Application.NewsFeedEngine;
using MicroBlogging.Application.Images;

namespace MicroBlogging.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<INewsFeedEngine, SimpleNewsFeedEngine>();
        services.AddSingleton<IImageResizer, ImageSharpResizer>();

        return services;
    }
}
