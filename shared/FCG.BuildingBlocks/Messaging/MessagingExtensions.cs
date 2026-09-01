using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.BuildingBlocks.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddFcgMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        string endpointPrefix,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        var section = configuration.GetSection("RabbitMq");
        var settings = section.Get<RabbitMqSettings>() ?? new RabbitMqSettings();

        services.AddHttpContextAccessor();

        services.AddMassTransit(x =>
        {
            // Sem prefixo, o nome da fila vem só do nome da classe do consumer: dois
            // serviços com consumers homônimos (PaymentProcessedConsumer existe no
            // catalog e no notifications) acabam competindo pela mesma fila e cada
            // mensagem chega a apenas um deles.
            x.SetEndpointNameFormatter(
                new KebabCaseEndpointNameFormatter(endpointPrefix, includeNamespace: false));

            configureConsumers?.Invoke(x);

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(settings.Host, settings.Port, settings.VirtualHost, h =>
                {
                    h.Username(settings.Username);
                    h.Password(settings.Password);
                });

                cfg.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15)));

                cfg.UsePublishFilter(typeof(CorrelationIdPublishFilter<>), ctx);
                cfg.UseConsumeFilter(typeof(CorrelationIdConsumeFilter<>), ctx);

                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
