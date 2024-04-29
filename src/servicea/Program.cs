using System.Reflection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using ServiceA.Configuration;
using ServiceA;




var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var cfg = new ServiceConfig();
builder.Configuration.GetSection(ServiceConfig.SECTION_NAME).Bind(cfg);
if (cfg == null || !cfg.IsValid())
{
    throw new Exception("Invalid configuration");
}

builder.Services.AddSingleton(cfg);
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
var resourceAttributes = new Dictionary<string, object> {
    { "service.name", "Service-A" },
    { "service.namespace", "pevo-namespace" },
    { "serviceVersion", typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown" },
    { "service.instance.id", Environment.MachineName }};




builder.Services.AddOpenTelemetry()
    /*options =>
    {
        // Set the sampling ratio to 10%. This means that 10% of all traces will be sampled and sent to Azure Monitor.
        options.SamplingRatio = 1.0F;
        options.ConnectionString = "InstrumentationKey=191a9ecb-1dde-4597-9f92-4d16a57883bd;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/;LiveEndpoint=https://westeurope.livediagnostics.monitor.azure.com/";
    });
    */
    //      .AddJaegerExporter() -- tracing
    .ConfigureResource(resourceBuilder => resourceBuilder
        .AddAttributes(resourceAttributes)
    )
    .WithTracing(traceBuilder =>  
        { 
            traceBuilder
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddProcessor<CustomProcessor>()
                .AddOtlpExporter(otlpOptions =>
                    {
                        // Use IConfiguration directly for Otlp exporter endpoint option.
                        otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue("Otlp:Endpoint", defaultValue: "http://localhost:4317")!);
                    })
                .AddConsoleExporter();
            builder.Services.Configure<AspNetCoreTraceInstrumentationOptions>(builder.Configuration.GetSection("AspNetCoreInstrumentation"));
        }
    )

    
    .WithMetrics(metricsBuilder => metricsBuilder
        .AddRuntimeInstrumentation()
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddPrometheusExporter(o => o
            .DisableTotalNameSuffixForCounters = true
        )


    );

/*
Action<ResourceBuilder> buildOpenTelemetryResource = builder => builder
        .AddService("Service A", serviceVersion: assemblyVersion, serviceInstanceId: Environment.MachineName)
        .Build();
*/

builder.Services.Configure<JaegerExporterOptions>(builder.Configuration.GetSection("Jaeger"));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/healthz/readiness");
app.MapHealthChecks("/healthz/liveness");


app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.Run();
