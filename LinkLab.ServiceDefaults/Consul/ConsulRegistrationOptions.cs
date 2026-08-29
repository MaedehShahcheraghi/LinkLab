namespace LinkLab.ServiceDefaults.Consul;

public sealed class ConsulRegistrationOptions
{
    public const string SectionName = "Consul";

    public bool Enabled { get; set; } = true;


    public string Address { get; set; } =
        "http://localhost:8500";

  
    public string ServiceName { get; set; } =
        string.Empty;


    public string? ServiceId { get; set; }


    public string ServiceScheme { get; set; } =
        "http";


    public string ServiceAddress { get; set; } =
        string.Empty;


    public int ServicePort { get; set; } = 8080;


    public string HealthCheckPath { get; set; } =
        "/health/ready";

 
    public int HealthCheckIntervalSeconds { get; set; } =
        10;

 
    public int HealthCheckTimeoutSeconds { get; set; } =
        5;

    public int DeregisterCriticalServiceAfterSeconds { get; set; } = 60;


    public int RegistrationRefreshSeconds { get; set; } =
        30;

  
    public string[] Tags { get; set; } = [];
}