using LinkLab.Identity.Api.Models;

namespace LinkLab.Identity.Api.Core.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles, long permissionMask);
}
