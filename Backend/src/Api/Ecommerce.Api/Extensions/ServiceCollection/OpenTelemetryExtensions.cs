using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Ecommerce.Application.Configuration.Models;

namespace Ecommerce.Api.Extensions.ServiceCollection;

public static class OpenTelemetryExtensions
{
    public static readonly ActivitySource ActivitySource = new("Ecommerce.Api");
    public static readonly Meter Meter = new("Ecommerce.Api");

    // Métricas personalizadas
    public static readonly Counter<int> OrdersCreated = Meter.CreateCounter<int>(
        "ecommerce_orders_created_total",
        "Total number of orders created");

    public static readonly Counter<int> ProductsViewed = Meter.CreateCounter<int>(
        "ecommerce_products_viewed_total",
        "Total number of products viewed");

    public static readonly Histogram<double> OrderProcessingTime = Meter.CreateHistogram<double>(
        "ecommerce_order_processing_duration_seconds",
        "Time taken to process an order");

    public static readonly Counter<int> PaymentAttempts = Meter.CreateCounter<int>(
        "ecommerce_payment_attempts_total",
        "Total number of payment attempts");

    public static readonly Counter<int> UserRegistrations = Meter.CreateCounter<int>(
        "ecommerce_user_registrations_total",
        "Total number of user registrations");

    // AddServiceDiscovery
    // AddOpenTelemetryTracing
    // AddMetrics
    // AddLogging

    public static IServiceCollection AddOpenTelemetryConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var telemetryOptions = configuration
            .GetSection(OpenTelemetryOptions.SectionName)
            .Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();

        services.Configure<OpenTelemetryOptions>(
            configuration.GetSection(OpenTelemetryOptions.SectionName));

        services.AddOpenTelemetry()
            .ConfigureResource(builder => builder
                .AddService(
                    serviceName: telemetryOptions.ServiceName,
                    serviceVersion: telemetryOptions.ServiceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                    ["service.instance.id"] = Environment.MachineName
                }))
            .WithTracing(builder => builder
                .AddSource(ActivitySource.Name)
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = httpContext =>
                    {
                        // Filtrar health checks y otros endpoints no relevantes
                        var path = httpContext.Request.Path.Value?.ToLower();
                        return !path?.Contains("/health") == true &&
                               !path?.Contains("/metrics") == true;
                    };
                    options.EnrichWithHttpRequest = (activity, httpRequest) =>
                    {
                        activity.SetTag("http.request.body.size", httpRequest.ContentLength);
                        activity.SetTag("user.id", httpRequest.HttpContext.User?.Identity?.Name);
                    };
                    options.EnrichWithHttpResponse = (activity, httpResponse) =>
                    {
                        activity.SetTag("http.response.body.size", httpResponse.ContentLength);
                    };
                })
                .AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                    {
                        activity.SetTag("http.client.method", httpRequestMessage.Method.ToString());
                    };
                })
                .AddConsoleExporter(options => options.Targets = ConsoleExporterOutputTargets.Console)
                .ConfigureExporters(telemetryOptions))

            .WithMetrics(builder =>
                {
                    builder
                         //.AddMeter(Meter.Name)
                         .AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel")
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        //.AddProcessInstrumentation()
                        .ConfigureMetricExporters(telemetryOptions);
                }
            );

        return services;
    }

    private static TracerProviderBuilder ConfigureExporters(this TracerProviderBuilder builder, OpenTelemetryOptions options)
    {
        // Console Exporter para desarrollo
        if (options.Console.Enabled)
        {
            builder.AddConsoleExporter();
        }

        // Jaeger Exporter
        if (!string.IsNullOrEmpty(options.Jaeger.Endpoint))
        {
            builder.AddJaegerExporter(jaegerOptions =>
            {
                jaegerOptions.Endpoint = new Uri(options.Jaeger.Endpoint);
            });
        }

        // Zipkin Exporter
        //if (!string.IsNullOrEmpty(options.Zipkin.Endpoint))
        //{
        //    builder.AddZipkinExporter(zipkinOptions =>
        //    {
        //        zipkinOptions.Endpoint = new Uri(options.Zipkin.Endpoint);
        //    });
        //}

        // OTLP Exporter (para Azure Monitor, etc.)
        //if (!string.IsNullOrEmpty(options.OTLP.Endpoint))
        //{
        //    builder.AddOtlpExporter(otlpOptions =>
        //    {
        //        otlpOptions.Endpoint = new Uri(options.OTLP.Endpoint);
        //        otlpOptions.Protocol = OtlpExportProtocol.Grpc;
        //    });
        //}

        return builder;
    }

    private static MeterProviderBuilder ConfigureMetricExporters(
        this MeterProviderBuilder builder,
        OpenTelemetryOptions options)
    {
        // Console Exporter para desarrollo
        if (options.Console.Enabled)
        {
            builder.AddConsoleExporter();
        }

        // OTLP Exporter para métricas
        //if (!string.IsNullOrEmpty(options.OTLP.Endpoint))
        //{
        //    builder.AddOtlpExporter(otlpOptions =>
        //    {
        //        otlpOptions.Endpoint = new Uri(options.OTLP.Endpoint);
        //        otlpOptions.Protocol = OtlpExportProtocol.Grpc;
        //    });
        //}

        return builder;
    }
}