using LinkLab.BuildingBlocks.Core.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace LinkLab.Identity.Api.Authorization.Requirements;

public sealed class PermissionRequirement(Permission permission) : IAuthorizationRequirement
{
    public Permission Permission { get; } = permission;
}
