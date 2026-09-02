using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace FCG.BuildingBlocks.Observability;

public static class ObservabilityExtensions
{
    /// <summary>
    /// Expõe métricas no formato Prometheus. A instrumentação de ASP.NET Core já
    /// fornece o histograma http.server.request.duration com as tags de rota e de
    /// status HTTP, que é o suficiente para latência, throughput e taxa de erro.
    /// </summary>
    public static IServiceCollection AddFcgObservability(this IServiceCollection services, string serviceName)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithMetrics(m => m
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddPrometheusExporter());

        return services;
    }

    /// <summary>
    /// Publica o endpoint /metrics, que o Prometheus consulta de dentro do cluster.
    /// Não é roteado pelo gateway: é tráfego de operação, não de negócio.
    /// </summary>
    public static WebApplication UseFcgObservability(this WebApplication app)
    {
        app.MapPrometheusScrapingEndpoint();
        return app;
    }
}
