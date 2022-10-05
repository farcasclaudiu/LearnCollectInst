using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

// Define some important constants to initialize tracing with
var serviceName = "LearnCollectInst.Api";
var serviceVersion = "1.0.0";

var resBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: serviceName, serviceVersion: serviceVersion);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// OLTP tracing
builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
{
    tracerProviderBuilder
    .AddConsoleExporter()
    .AddSource(serviceName)
    .SetResourceBuilder(resBuilder)
    .AddHttpClientInstrumentation()
    .AddAspNetCoreInstrumentation()
    .AddSqlClientInstrumentation()
    .AddOtlpExporter(config =>
    {
        config.Endpoint = new Uri("http://otel-collector:4317");
    })
    .AddConsoleExporter();
});

// OTPL metrics
builder.Services.AddOpenTelemetryMetrics(config =>
{
    config.SetResourceBuilder(resBuilder);
    config.AddRuntimeInstrumentation()
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation();
    config.AddOtlpExporter(otlpOptions =>
        {
            otlpOptions.Endpoint = new Uri("http://otel-collector:4317");
        }).AddConsoleExporter();
});

// OTLP logger
builder.Host.ConfigureLogging(logging =>
    logging.ClearProviders()
    .AddOpenTelemetry(options =>
    {
        options.SetResourceBuilder(resBuilder);
        // Export the body of the message
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
        options.ParseStateValues = true;
        // exporters
        options.AddOtlpExporter(otlpOptions =>
        {
            otlpOptions.Endpoint = new Uri("http://otel-collector:4317");
        })
        .AddConsoleExporter();
    })
);


var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();
// app.UseHttpLogging();

var MyActivitySource = new ActivitySource(serviceName);

app.Run();
