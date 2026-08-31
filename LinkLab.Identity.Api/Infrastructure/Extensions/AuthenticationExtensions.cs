using System.Text;
using LinkLab.BuildingBlocks.Core.Authorization;
using LinkLab.Identity.Api.Authorization.Handlers;
using LinkLab.Identity.Api.Authorization.Requirements;
using LinkLab.Identity.Api.Core.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LinkLab.Identity.Api.Infrastructure.Extensions;

public static class AuthenticationExtensions
{
    public static WebApplicationBuilder AddJwtAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var jwtOptions = builder.Services
                    .BuildServiceProvider()
                    .GetRequiredService<IOptions<JwtOptions>>()
                    .Value;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey  = true,
                    ValidIssuer              = jwtOptions.Issuer,
                    ValidAudience            = jwtOptions.Audience,
                    IssuerSigningKey         = new SymmetricSecurityKey(
                                                 Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ClockSkew                = TimeSpan.Zero // Enterprise strict expiration
                };
            });

        // Register handler as a singleton — it holds no state.
        builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // Dynamically register a Policy for every Single-Bit Permission enum value.
        builder.Services.AddAuthorization(options =>
        {
            foreach (Permission permission in Enum.GetValues<Permission>())
            {
                if (!IsSingleBit(permission)) continue;

                options.AddPolicy(
                    $"Permission:{permission}",
                    policy => policy
                        .RequireAuthenticatedUser()
                        .AddRequirements(new PermissionRequirement(permission)));
            }
        });

        return builder;
    }

    private static bool IsSingleBit(Permission permission)
    {
        var value = (ulong)permission;
        return value != 0 && (value & (value - 1)) == 0;
    }
}
