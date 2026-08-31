using System.ComponentModel.DataAnnotations.Schema;
using LinkLab.BuildingBlocks.Core.Authorization;
using Microsoft.AspNetCore.Identity;

namespace LinkLab.Identity.Api.Models;

public class ApplicationRole : IdentityRole<Guid>
{
    public long PermissionMask { get; private set; }


    [NotMapped]
    public Permission Permissions =>
        (Permission)PermissionMask;

    public void SetPermissions(
        Permission permissions)
    {
        PermissionMask = (long)permissions;
    }

    public void Grant(
        Permission permission)
    {
        PermissionMask |= (long)permission;
    }

    public void Revoke(
        Permission permission)
    {
        PermissionMask &= ~(long)permission;
    }

    public bool HasPermission(
        Permission permission)
    {
        return ((Permission)PermissionMask)
            .HasAll(permission);
    }
}