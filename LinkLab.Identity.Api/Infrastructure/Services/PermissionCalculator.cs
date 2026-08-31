using LinkLab.Identity.Api.Core.Interfaces;
using LinkLab.Identity.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace LinkLab.Identity.Api.Infrastructure.Services;

public sealed class PermissionCalculator(IdentityDbContext dbContext) : IPermissionCalculator
{
    public async Task<long> CalculateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var masks = await (
            from userRole in dbContext.UserRoles
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == userId
            select role.PermissionMask
        ).ToListAsync(cancellationToken);

        return masks.Aggregate(0L, (acc, mask) => acc | mask);
    }
}
