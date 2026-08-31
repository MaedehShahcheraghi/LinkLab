using LinkLab.BuildingBlocks.Core.Authorization;
using LinkLab.Identity.Api.Authorization.Requirements;
using LinkLab.Identity.Api.Core.Constants;
using Microsoft.AspNetCore.Authorization;

namespace LinkLab.Identity.Api.Authorization.Handlers;

public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var permissionClaim = context.User
            .FindFirst(c => c.Type == CustomClaims.Permission);

        if (permissionClaim is null || !long.TryParse(permissionClaim.Value, out var mask))
        {
            return Task.CompletedTask;
        }

        var userPermissions = (Permission)mask;

        // Since Permission is flags, we check if it has the required permission
        if ((userPermissions & requirement.Permission) == requirement.Permission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
