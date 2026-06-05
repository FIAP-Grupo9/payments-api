using MassTransit;
using Microsoft.AspNetCore.Http;
using FCG.BuildingBlocks.Middleware;
using LogContext = Serilog.Context.LogContext;

namespace FCG.BuildingBlocks.Messaging;

public class CorrelationIdPublishFilter<T> : IFilter<PublishContext<T>> where T : class
{
    private readonly IHttpContextAccessor _http;

    public CorrelationIdPublishFilter(IHttpContextAccessor http) => _http = http;

    public Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
    {
        var ctx = _http.HttpContext;
        if (ctx is not null && ctx.Items.TryGetValue("CorrelationId", out var cid) && cid is string s)
        {
            context.Headers.Set(CorrelationIdMiddleware.HeaderName, s);
        }
        else
        {
            // Background publishes (from consumer republishing) may have no HttpContext.
            // The consume filter will surface the source header when present; nothing to do here.
        }
        return next.Send(context);
    }

    public void Probe(ProbeContext context) => context.CreateFilterScope("correlationId-publish");
}

public class CorrelationIdConsumeFilter<T> : IFilter<ConsumeContext<T>> where T : class
{
    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var cid = context.Headers.Get<string>(CorrelationIdMiddleware.HeaderName)
                  ?? Guid.NewGuid().ToString();
        using (LogContext.PushProperty("CorrelationId", cid))
        {
            await next.Send(context);
        }
    }

    public void Probe(ProbeContext context) => context.CreateFilterScope("correlationId-consume");
}
