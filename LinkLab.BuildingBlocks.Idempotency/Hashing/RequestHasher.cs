using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;

namespace LinkLab.BuildingBlocks.Idempotency;

public sealed class RequestHasher
{
    public async Task<string> HashRequestAsync(
        HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        request.Body.Position = 0;

        using var ms = new MemoryStream();
        await request.Body.CopyToAsync(ms, cancellationToken);
        request.Body.Position = 0;

        var hash = SHA256.HashData(ms.ToArray());
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
