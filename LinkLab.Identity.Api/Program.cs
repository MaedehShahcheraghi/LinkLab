using LinkLab.BuildingBlocks.Core.Middleware;
using LinkLab.BuildingBlocks.Idempotency;
using LinkLab.Identity.Api.Data;
using LinkLab.Identity.Api.Infrastructure.Extensions;
using LinkLab.Identity.Api.Models;
using LinkLab.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Infrastructure ──────────────────────────────────────────────
builder.AddServiceDefaults();

// ── Persistence ─────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("IdentityDb")
    ?? throw new InvalidOperationException("Connection string 'IdentityDb' is missing.");

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
        sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null)));

builder.Services.AddDbContextFactory<IdentityDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
        sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null)),
    ServiceLifetime.Scoped);

// ── ASP.NET Core Identity ───────────────────────────────────────
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength      = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail      = true;
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<IdentityDbContext>();

// ── Options ─────────────────────────────────────────────────────
builder.Services
    .AddOptions<LinkLab.Identity.Api.Core.Options.JwtOptions>()
    .BindConfiguration(LinkLab.Identity.Api.Core.Options.JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ── JWT Authentication & Permission-based Authorization ─────────
builder.AddJwtAuthentication();

// ── Application Services ────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<LinkLab.Identity.Api.Core.Interfaces.ITokenContext, LinkLab.Identity.Api.Infrastructure.Services.HttpContextTokenContext>();

builder.Services.AddScoped<LinkLab.Identity.Api.Core.Interfaces.IUserRepository,
    LinkLab.Identity.Api.Infrastructure.Repositories.UserRepository>();

builder.Services.AddScoped<LinkLab.Identity.Api.Core.Interfaces.IPermissionCalculator,
    LinkLab.Identity.Api.Infrastructure.Services.PermissionCalculator>();

builder.Services.AddScoped<LinkLab.Identity.Api.Core.Interfaces.ITokenService,
    LinkLab.Identity.Api.Infrastructure.Services.TokenService>();

builder.Services.AddScoped<LinkLab.Identity.Api.Core.Interfaces.IRefreshTokenService,
    LinkLab.Identity.Api.Infrastructure.Services.RefreshTokenService>();

builder.Services.AddScoped<LinkLab.Identity.Api.Core.Interfaces.IAuthService,
    LinkLab.Identity.Api.Application.Services.AuthService>();

builder.Services.AddScoped<LinkLab.Identity.Api.Core.Interfaces.IUnitOfWork,
    LinkLab.Identity.Api.Infrastructure.Data.UnitOfWork>();

// ── Idempotency (SQL + Redis cache) ─────────────────────────────
builder.AddLinkLabIdempotency<IdentityDbContext>(builder.Configuration);

// ── API ─────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ────────────────────────────────────────────────────────────────
var app = builder.Build();

// Must be first — catches all unhandled exceptions before any other middleware runs
app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();

app.MapServiceDefaults();

app.UseAuthentication();
app.UseAuthorization();

app.UseIdempotencyFingerprint();

app.MapControllers();

app.Run();