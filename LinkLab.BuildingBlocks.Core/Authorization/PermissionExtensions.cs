namespace LinkLab.BuildingBlocks.Core.Authorization;

public static class PermissionExtensions
{
    public static Permission AllPermissions =>
        Enum.GetValues<Permission>()
            .Where(x => x != Permission.None && IsSingleBit(x))
            .Aggregate(Permission.None, (current, next) => current | next);

    private static bool IsSingleBit(Permission permission)
    {
        var value = (ulong)permission;
        return value != 0 && (value & (value - 1)) == 0;
    }

    public static bool HasAll(this Permission current, Permission required)
        => (current & required) == required;

    public static bool HasAny(this Permission current, Permission required)
        => (current & required) != 0;
}
