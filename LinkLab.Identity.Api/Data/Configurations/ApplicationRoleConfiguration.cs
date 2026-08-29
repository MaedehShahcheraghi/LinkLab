using LinkLab.Identity.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkLab.Identity.Api.Data.Configurations;

internal sealed class ApplicationRoleConfiguration
    : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {

        builder.Property(r => r.PermissionMask)
            .IsRequired()
            .HasDefaultValue(0L);
    }
}
