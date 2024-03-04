using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RinhaBackend.DbPostgresql;

public static class ProgramExtensions
{
    public static IServiceCollection AddAndConfigureRequestTimeout(
        this IServiceCollection services,
         IConfiguration configuration)
    {
        if ((configuration["USE_TIMEOUT_MID"] ?? bool.FalseString) == bool.TrueString)
        {
            services.AddRequestTimeouts();
        }

        return services;
    }

    public static IServiceCollection AddAndConfigureHealthChecks(
        this IServiceCollection services,
         IConfiguration configuration)
    {
        if ((configuration["USE_HEALTHZ"] ?? bool.FalseString) == bool.TrueString)
        {
            services.AddHealthChecks()
                .AddNpgSql(connectionString: configuration.GetConnectionString("Default"));
        }

        return services;
    }

    public static IApplicationBuilder IncludeRequestTimeout(
        this IApplicationBuilder builder,
            IConfiguration configuration)
    {
        if ((configuration["USE_TIMEOUT_MID"] ?? bool.FalseString) == bool.TrueString)
            builder.UseRequestTimeouts();

        return builder;
    }

    public static IApplicationBuilder IncludeHealhCheck(
        this IApplicationBuilder builder,
        IConfiguration configuration)
    {
        if ((configuration["USE_HEALTHZ"] ?? bool.FalseString) == bool.TrueString)
            builder.UseHealthChecks("/healthz", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = async (context, report) =>
                {
                    var result = JsonSerializer.Serialize(
                        new
                        {
                            status = report.Status.ToString(),
                            monitors = report.Entries.Select(e => new { key = e.Key, value = Enum.GetName(typeof(HealthStatus), e.Value.Status) })
                        });
                    context.Response.ContentType = MediaTypeNames.Application.Json;
                    await context.Response.WriteAsync(result);
                }
            });

        return builder;
    }
}
