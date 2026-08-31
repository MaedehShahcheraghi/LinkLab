using LinkLab.BuildingBlocks.Core.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace LinkLab.Identity.Api.Authorization.Attributes;

public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(Permission permission)
        : base(policy: $"Permission:{permission}")
    {
    }
}
