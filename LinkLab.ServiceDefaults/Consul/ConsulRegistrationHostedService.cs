using Consul;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkLab.ServiceDefaults.Consul;

internal sealed class ConsulRegistrationHostedService
    : BackgroundService
{
    private readonly IConsulClient _consulClient;

    private readonly ILogger<
        ConsulRegistrationHostedService> _logger;

    private readonly ConsulRegistrationOptions _options;

    private readonly string _serviceId;

    private bool _hasRegisteredSuccessfully;

    public ConsulRegistrationHostedService(
        IConsulClient consulClient,
        IOptions<ConsulRegistrationOptions> options,
        ILogger<ConsulRegistrationHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(consulClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _consulClient = consulClient;
        _options = options.Value;
        _logger = logger;

        _serviceId = ResolveServiceId(_options);
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "Consul service registration is disabled.");

            return;
        }

        await RegisterServiceAsync(stoppingToken);

        if (_options.RegistrationRefreshSeconds == 0) return;

        var refreshInterval = TimeSpan.FromSeconds(
            _options.RegistrationRefreshSeconds);

        using var timer = new PeriodicTimer(refreshInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(
                       stoppingToken))
                await RegisterServiceAsync(stoppingToken);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Application is shutting down.
        }
    }

    public override async Task StopAsync(
        CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (!_options.Enabled ||
            !_hasRegisteredSuccessfully)
            return;

        await DeregisterServiceAsync(cancellationToken);
    }

    private async Task RegisterServiceAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var registration = CreateRegistration();

            await _consulClient.Agent.ServiceRegister(
                registration,
                cancellationToken);

            if (!_hasRegisteredSuccessfully)
                _logger.LogInformation(
                    "Service {ServiceName} with ID {ServiceId} " +
                    "was registered in Consul.",
                    _options.ServiceName,
                    _serviceId);

            _hasRegisteredSuccessfully = true;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Could not register service {ServiceName} " +
                "with ID {ServiceId} in Consul.",
                _options.ServiceName,
                _serviceId);
        }
    }

    private async Task DeregisterServiceAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await _consulClient.Agent.ServiceDeregister(
                _serviceId,
                cancellationToken);

            _hasRegisteredSuccessfully = false;

            _logger.LogInformation(
                "Service {ServiceName} with ID {ServiceId} " +
                "was deregistered from Consul.",
                _options.ServiceName,
                _serviceId);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Consul deregistration for service {ServiceId} " +
                "was cancelled because the application shutdown " +
                "timeout was reached.",
                _serviceId);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Could not deregister service {ServiceName} " +
                "with ID {ServiceId} from Consul.",
                _options.ServiceName,
                _serviceId);
        }
    }

    private AgentServiceRegistration CreateRegistration()
    {
        return new AgentServiceRegistration
        {
            ID = _serviceId,

            Name = _options.ServiceName,

            Address = _options.ServiceAddress,

            Port = _options.ServicePort,

            Tags = CreateTags(),

            Check = CreateHealthCheck()
        };
    }

    private AgentServiceCheck CreateHealthCheck()
    {
        return new AgentServiceCheck
        {
            HTTP = CreateHealthCheckUri().ToString(),

            Interval = TimeSpan.FromSeconds(
                _options.HealthCheckIntervalSeconds),

            Timeout = TimeSpan.FromSeconds(
                _options.HealthCheckTimeoutSeconds),

            DeregisterCriticalServiceAfter =
                TimeSpan.FromSeconds(
                    _options
                        .DeregisterCriticalServiceAfterSeconds)
        };
    }

    private Uri CreateHealthCheckUri()
    {
        var uriBuilder = new UriBuilder
        {
            Scheme = _options.ServiceScheme,

            Host = _options.ServiceAddress,

            Port = _options.ServicePort,

            Path = _options.HealthCheckPath
        };

        return uriBuilder.Uri;
    }

    private string[] CreateTags()
    {
        return _options.Tags
            .Where(tag =>
                !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string ResolveServiceId(
        ConsulRegistrationOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ServiceId)) return options.ServiceId.Trim();

        return string.Join(
            '-',
            options.ServiceName,
            Environment.MachineName,
            options.ServicePort);
    }
}