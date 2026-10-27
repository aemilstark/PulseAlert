using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PulseAlert.Infrastructure.Data;

namespace PulseAlert.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "PulseAlert";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<PulseAlertDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
