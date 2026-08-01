using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace LinkLab.ServiceDefaults;

public static class ResilientHttpClientExtensions
{
    public static IHttpClientBuilder AddLinkLabResilienceHandler(
        this IHttpClientBuilder builder)
    {
        builder.AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 2;
            options.Retry.Delay = TimeSpan.FromMilliseconds(250);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;

            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.MinimumThroughput = 10;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
        });

        return builder;
    }


    public static IHttpClientBuilder AddResilientHttpClient<
        TClient,
        TImplementation>(
        this IServiceCollection services,
        Action<HttpClient> configureClient)
        where TClient : class
        where TImplementation : class, TClient
    {
        return
            services.AddHttpClient<TClient, TImplementation>(configureClient).AddLinkLabResilienceHandler();
    }
}