namespace LinkLab.Identity.Api.Authorization;

public static class PermissionExtensions
{
    public static bool HasAll(
        this Permission current,
        Permission required)
    {
        if (required == Permission.None) return true;

        return (current & required) == required;
    }

    public static bool HasAny(
        this Permission current,
        Permission required)
    {
        if (required == Permission.None) return true;

        return (current & required) != Permission.None;
    }

    public static Permission Add(
        this Permission current,
        Permission permission)
    {
        return current | permission;
    }

    public static Permission Remove(
        this Permission current,
        Permission permission)
    {
        return current & ~permission;
    }
}