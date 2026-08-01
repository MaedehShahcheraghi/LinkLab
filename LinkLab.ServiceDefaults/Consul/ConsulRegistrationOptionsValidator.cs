using Microsoft.Extensions.Options;

namespace LinkLab.ServiceDefaults.Consul;

public class ConsulRegistrationOptionsValidator : IValidateOptions<ConsulRegistrationOptions>
{
    public ValidateOptionsResult Validate(string? name, ConsulRegistrationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Enabled) return ValidateOptionsResult.Success;

        var failures = new List<string>();

        ValidateConsulAddress(options, failures);
        ValidateServiceInformation(options, failures);
        ValidateHealthCheck(options, failures);
        ValidateRegistrationRefresh(options, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateConsulAddress(
        ConsulRegistrationOptions options,
        ICollection<string> failures)
    {
        if (!Uri.TryCreate(
                options.Address,
                UriKind.Absolute,
                out var consulUri))
        {
            failures.Add(
                "Consul:Address must be a valid absolute URI.");

            return;
        }

        if (!IsHttpScheme(consulUri.Scheme))
            failures.Add(
                "Consul:Address scheme must be http or https.");

        if (string.IsNullOrWhiteSpace(consulUri.Host))
            failures.Add(
                "Consul:Address must contain a valid host.");
    }

    private static void ValidateServiceInformation(
        ConsulRegistrationOptions options,
        ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(
                options.ServiceName))
            failures.Add(
                "Consul:ServiceName is required.");

        if (string.IsNullOrWhiteSpace(
                options.ServiceAddress))
            failures.Add(
                "Consul:ServiceAddress is required.");

        if (options.ServicePort is <= 0 or > 65535)
            failures.Add(
                "Consul:ServicePort must be between 1 and 65535.");

        if (!IsHttpScheme(options.ServiceScheme))
            failures.Add(
                "Consul:ServiceScheme must be http or https.");
    }

    private static void ValidateHealthCheck(
        ConsulRegistrationOptions options,
        ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(
                options.HealthCheckPath))
            failures.Add(
                "Consul:HealthCheckPath is required.");
        else if (!options.HealthCheckPath.StartsWith(
                     "/",
                     StringComparison.Ordinal))
            failures.Add(
                "Consul:HealthCheckPath must start with '/'.");

        if (options.HealthCheckIntervalSeconds < 1)
            failures.Add(
                "Consul:HealthCheckIntervalSeconds must be at least 1.");

        if (options.HealthCheckTimeoutSeconds < 1)
            failures.Add(
                "Consul:HealthCheckTimeoutSeconds must be at least 1.");

        if (options.HealthCheckTimeoutSeconds >
            options.HealthCheckIntervalSeconds)
            failures.Add(
                "Consul:HealthCheckTimeoutSeconds cannot be greater " +
                "than HealthCheckIntervalSeconds.");

        if (options.DeregisterCriticalServiceAfterSeconds < 1)
            failures.Add(
                "Consul:DeregisterCriticalServiceAfterSeconds " +
                "must be at least 1.");
    }

    private static void ValidateRegistrationRefresh(
        ConsulRegistrationOptions options,
        ICollection<string> failures)
    {
        if (options.RegistrationRefreshSeconds < 0)
            failures.Add(
                "Consul:RegistrationRefreshSeconds cannot be negative.");
    }

    private static bool IsHttpScheme(string? scheme)
    {
        return string.Equals(
                   scheme,
                   Uri.UriSchemeHttp,
                   StringComparison.OrdinalIgnoreCase)
               ||
               string.Equals(
                   scheme,
                   Uri.UriSchemeHttps,
                   StringComparison.OrdinalIgnoreCase);
    }
}