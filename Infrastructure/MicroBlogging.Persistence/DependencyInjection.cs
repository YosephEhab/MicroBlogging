using MicroBlogging.Domain.Repositories;
using MicroBlogging.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MicroBlogging.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<MicroBloggingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("MicroBloggingConnection")));

        services.AddBlobStorage(configuration);

        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IPostRepository, PostRepository>();

        return services;
    }

    private static IServiceCollection AddBlobStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AzureBlobStorage");

        services.AddSingleton<IImageStorage>(new AzureBlobImageStorage(connectionString!, "images"));

        return services;
    }
}
