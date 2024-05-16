using System.Reflection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using OpenTelemetry.Instrumentation.AspNetCore;
using System.Diagnostics.Metrics;



using ServiceB;
using Azure.Monitor.OpenTelemetry.AspNetCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
var resourceAttributes = new Dictionary<string, object> {
    { "service.name", "Service-A" },
    { "service.namespace", "pevo-namespace" },
    { "serviceVersion", assemblyVersion },
    { "service.instance.id", Environment.MachineName }};



/*
Action<ResourceBuilder> configureResource = r => r.AddService(
    serviceName: builder.Configuration.GetValue("ServiceName", defaultValue: "otel-test")!,
    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
    serviceInstanceId: Environment.MachineName);
    */

// Create a service to expose ActivitySource, and Metric Instruments
// for manual instrumentation
//TODO
//builder.Services.AddSingleton<Instrumentation>();




builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(options =>
{
    options
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddAttributes(resourceAttributes)
        )
        .AddOtlpExporter()
        .AddConsoleExporter();
});


//app.Logger.LogInformation("Adding Routes");

//var logger = builder.Logging.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

//logger.LogInformation("Configured Logging");
/*
builder.Services.Configure<OpenTelemetryLoggerOptions>(opt =>
{
    opt.IncludeScopes = true;
    opt.ParseStateValues = true;
    opt.IncludeFormattedMessage = true;
    
});
*/


builder.Services.Configure<OpenTelemetryLoggerOptions>(opt =>
{
    opt.IncludeScopes = true;
    opt.ParseStateValues = true;
    opt.IncludeFormattedMessage = true;
    
});

builder.Services.AddOpenTelemetry( )
    
    .UseAzureMonitor()
    .ConfigureResource(resourceBuilder => resourceBuilder
        .AddAttributes(resourceAttributes)
    )
   

    .WithTracing(traceBuilder =>  
        { 
            traceBuilder
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddProcessor<CustomProcessor>()
                .AddSource(CustomTraces.Default.Name)
                .AddConsoleExporter();

            
            builder.Services.Configure<AspNetCoreTraceInstrumentationOptions>(builder.Configuration.GetSection("AspNetCoreInstrumentation"));
            traceBuilder.AddOtlpExporter(otlpOptions =>
            {
                // Use IConfiguration directly for Otlp exporter endpoint option.
                otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue("Otlp:Endpoint", defaultValue: "http://localhost:4317")!);
            });
            //builder.Services.Configure<JaegerExporterOptions>(builder.Configuration.GetSection("Jaeger"));
        }
    )

    
    .WithMetrics(metricsBuilder => metricsBuilder
        .AddRuntimeInstrumentation()
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddMeter(CustomMetrics.Default.Name)
        .AddView(CustomMetrics.PingDelay.Name, CustomMetrics.PingDelayView)
        .AddOtlpExporter()
        .AddPrometheusExporter(o => o
            .DisableTotalNameSuffixForCounters = true
        )

    );
    


//logger.LogInformation("Configured Traces");


//logger.LogInformation("Configured Metrics");
//logger.LogInformation("Registering Middlewares");
var app = builder.Build();
app.Logger.LogInformation(" - Swagger");
app.UseSwagger();
app.Logger.LogInformation(" - SwaggerUI");
app.UseSwaggerUI();

app.Logger.LogInformation(" - Authorization");
app.UseAuthorization();
app.Logger.LogInformation(" - API Controllers");
app.MapControllers();
app.Logger.LogInformation(" - HealthChecks");
app.MapHealthChecks("/healthz/readiness");
app.MapHealthChecks("/healthz/liveness");

app.Logger.LogInformation(" - Prometheus Scraping Endpoint");
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.Logger.LogInformation("Middlewares registered");
app.Logger.LogInformation("Starting API...");
app.Run();
