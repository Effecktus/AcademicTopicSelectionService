using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using DirectoryOfGraduates.Infrastructure.Data;
using DirectoryOfGraduates.Application.Abstractions;
using DirectoryOfGraduates.Infrastructure.Repositories;

namespace DirectoryOfGraduates.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUserRolesRepository, UserRolesRepository>();

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = BuildPostgresConnectionString(configuration);
            options.UseNpgsql(connectionString);
        });

        return services;
    }

    private static string BuildPostgresConnectionString(IConfiguration config)
    {
        var cs = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(cs))
        {
            throw new InvalidOperationException("Missing connection string: ConnectionStrings:DefaultConnection");
        }

        var csb = new NpgsqlConnectionStringBuilder(cs);

        // If password already provided, keep it.
        if (!string.IsNullOrWhiteSpace(csb.Password))
        {
            return csb.ConnectionString;
        }

        var passwordFile = config["Db:PasswordFile"];
        if (string.IsNullOrWhiteSpace(passwordFile))
        {
            return csb.ConnectionString;
        }

        if (!File.Exists(passwordFile))
        {
            throw new FileNotFoundException($"Password file not found: '{passwordFile}'", passwordFile);
        }

        var password = File.ReadAllText(passwordFile).Trim();
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException($"Password file is empty: '{passwordFile}'");
        }

        csb.Password = password;
        return csb.ConnectionString;
    }
}

