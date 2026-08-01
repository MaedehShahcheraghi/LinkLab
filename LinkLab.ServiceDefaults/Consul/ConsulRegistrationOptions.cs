namespace LinkLab.ServiceDefaults.Consul;

public sealed class ConsulRegistrationOptions
{
    public const string SectionName = "Consul";

    /// <summary>
    ///     مشخص می‌کند ثبت سرویس در Consul فعال باشد یا نه.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     آدرس Consul Agent.
    /// </summary>
    public string Address { get; set; } =
        "http://localhost:8500";

    /// <summary>
    ///     نام منطقی سرویس در Consul.
    ///     مثال: authentication-service
    /// </summary>
    public string ServiceName { get; set; } =
        string.Empty;

    /// <summary>
    ///     شناسه یکتای Instance سرویس.
    ///     اگر مقداردهی نشود، به‌صورت خودکار ساخته می‌شود.
    /// </summary>
    public string? ServiceId { get; set; }

    /// <summary>
    ///     Scheme مربوط به خود سرویس.
    /// </summary>
    public string ServiceScheme { get; set; } =
        "http";

    /// <summary>
    ///     آدرسی که Consul برای دسترسی به سرویس استفاده می‌کند.
    ///     مثال:
    ///     localhost
    ///     authentication-api
    ///     192.168.1.20
    /// </summary>
    public string ServiceAddress { get; set; } =
        string.Empty;

    /// <summary>
    ///     پورت سرویس.
    /// </summary>
    public int ServicePort { get; set; } = 8080;

    /// <summary>
    ///     مسیر Health Check سرویس.
    /// </summary>
    public string HealthCheckPath { get; set; } =
        "/health/ready";

    /// <summary>
    ///     فاصله اجرای Health Check توسط Consul.
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } =
        10;

    /// <summary>
    ///     حداکثر زمان انتظار Consul برای Health Check.
    /// </summary>
    public int HealthCheckTimeoutSeconds { get; set; } =
        5;

    /// <summary>
    ///     اگر سرویس برای این مدت Critical باقی بماند،
    ///     Consul آن را به‌صورت خودکار حذف می‌کند.
    /// </summary>
    public int DeregisterCriticalServiceAfterSeconds { get; set; } = 60;

    /// <summary>
    ///     فاصله تلاش مجدد برای اطمینان از ثبت بودن سرویس.
    ///     این قابلیت در صورت Restart شدن Consul Agent
    ///     باعث ثبت مجدد سرویس می‌شود.
    ///     مقدار صفر یعنی فقط یک بار ثبت شود.
    /// </summary>
    public int RegistrationRefreshSeconds { get; set; } =
        30;

    /// <summary>
    ///     Tagهای سرویس در Consul.
    /// </summary>
    public string[] Tags { get; set; } = [];
}