using System.Text;
using LinkLab.BuildingBlocks.Core.Authorization;
using LinkLab.Identity.Api.Authorization.Handlers;
using LinkLab.Identity.Api.Authorization.Requirements;
using LinkLab.Identity.Api.Core.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LinkLab.Identity.Api.Infrastructure.Extensions;

public static class AuthenticationExtensions
{
    public static WebApplicationBuilder AddJwtAuthentication(
        this WebApplicationBuilder builder)
    {
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        builder.Services
            .AddOptions<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>(
                (options, jwtOptions) =>
                {
                    var jwt = jwtOptions.Value;

                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,

                            ValidIssuer = jwt.Issuer,
                            ValidAudience = jwt.Audience,

                            IssuerSigningKey =
                                new SymmetricSecurityKey(
                                    Encoding.UTF8.GetBytes(jwt.SecretKey)),

                            ClockSkew = TimeSpan.Zero
                        };
                });


        builder.Services.AddSingleton<
            IAuthorizationHandler,
            PermissionAuthorizationHandler>();


        builder.Services.AddAuthorization(options =>
        {
            foreach (Permission permission in Enum.GetValues<Permission>())
            {
                if (!IsSingleBit(permission))
                    continue;


                options.AddPolicy(
                    $"Permission:{permission}",
                    policy =>
                    {
                        policy
                            .RequireAuthenticatedUser()
                            .AddRequirements(
                                new PermissionRequirement(permission));
                    });
            }
        });


        return builder;
    }


    private static bool IsSingleBit(Permission permission)
    {
        var value = (ulong)permission;

        return value != 0 &&
               (value & (value - 1)) == 0;
    }
}