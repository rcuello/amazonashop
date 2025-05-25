using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.Configuration.Models;

public class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    public string ServiceName { get; set; } = "ecommerce-api";
    public string ServiceVersion { get; set; } = "1.0.0";
    public JaegerOptions Jaeger { get; set; } = new();
    public ZipkinOptions Zipkin { get; set; } = new();
    public OtlpOptions OTLP { get; set; } = new();
    public ConsoleOptions Console { get; set; } = new();
}
