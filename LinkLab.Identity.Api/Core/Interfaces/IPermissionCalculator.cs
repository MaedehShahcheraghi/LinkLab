namespace LinkLab.Identity.Api.Core.Interfaces;

public interface IPermissionCalculator
{
    Task<long> CalculateAsync(Guid userId, CancellationToken cancellationToken = default);
}
