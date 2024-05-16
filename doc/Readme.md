
# Launch Profiles

Create a file:

- Properties/launchSettings.json

Go to project service a and b and run:

```
dotnet run --launch-profile https
```

# Prometheus

## Add new targets / services to Prometheus

__FILE__ : /etc/prometheus/prometheus.yml
__FILE_CONTENT__
```
  - job_name: 'serviceA'
    scrape_interval: 5s # Poll every 5 seconds for a more responsive demo.
    static_configs:
      - targets: ["localhost:5089", ]  ## Enter the HTTP port number of the demo app.

  - job_name: 'serviceB'
    scrape_interval: 5s # Poll every 5 seconds for a more responsive demo.
    static_configs:
      - targets: ["localhost:5100", ]  ## Enter the HTTP port number of the demo app.
```
## Configure service


- [opentelemetry prometheus](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Prometheus.AspNetCore/README.md)


### sharing port and path with the serivce

it becomes: http://localhost:5100/metrics

```
app.UseOpenTelemetryPrometheusScrapingEndpoint()
```

You MAY also set scrapping endpoint differently from the service itself
```
app.UseOpenTelemetryPrometheusScrapingEndpoint(
        context => context.Request.Path == "/internal/metrics"
            && context.Connection.LocalPort == 5067);

```


## Advanced

[historgrams](https://www.asserts.ai/blog/opentelemetry-histograms-with-prometheus/)