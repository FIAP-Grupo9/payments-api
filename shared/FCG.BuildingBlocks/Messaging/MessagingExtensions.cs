using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.BuildingBlocks.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddFcgMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        var section = configuration.GetSection("RabbitMq");
        var settings = section.Get<RabbitMqSettings>() ?? new RabbitMqSettings();

        services.AddHttpContextAccessor();

        services.AddMassTransit(x =>
        {
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
