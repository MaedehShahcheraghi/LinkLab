namespace LinkLab.Identity.Api.Authorization;

[Flags]
public enum Permission : long
{
    None = 0,

    ProfileRead = 1L << 0,
    ProfileUpdate = 1L << 1,

    ShortLinksReadOwn = 1L << 2,
    ShortLinksCreate = 1L << 3,
    ShortLinksUpdateOwn = 1L << 4,
    ShortLinksDeleteOwn = 1L << 5,

    AnalyticsReadOwn = 1L << 6,

    UsersRead = 1L << 7,
    UsersManage = 1L << 8,

    RolesRead = 1L << 9,
    RolesManage = 1L << 10,

    All =
        ProfileRead |
        ProfileUpdate |
        ShortLinksReadOwn |
        ShortLinksCreate |
        ShortLinksUpdateOwn |
        ShortLinksDeleteOwn |
        AnalyticsReadOwn |
        UsersRead |
        UsersManage |
        RolesRead |
        RolesManage
}