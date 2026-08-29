using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkLab.BuildingBlocks.Idempotency;

public sealed class IdempotencyRecordConfiguration
    : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Scope)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Key)
            .IsRequired()
            .HasMaxLength(256)
            .UseCollation("Latin1_General_100_BIN2");

        builder.Property(r => r.RequestHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(r => r.AttemptId)
            .IsRequired()
            .IsConcurrencyToken();

        builder.Property(r => r.State)
            .IsRequired()
            .IsConcurrencyToken();

        builder.Property(r => r.ContentType)
            .HasMaxLength(200);

        builder.Property(r => r.CreatedAtUtc)
            .IsRequired();

        builder.Property(r => r.ExpiresAtUtc)
            .IsRequired();

        builder.HasIndex(r => new { r.Scope, r.Key })
            .IsUnique()
            .HasDatabaseName("IX_IdempotencyRecords_Scope_Key");

        builder.HasIndex(r => r.ExpiresAtUtc)
            .HasDatabaseName("IX_IdempotencyRecords_ExpiresAtUtc");
    }
}

public static class IdempotencyModelBuilderExtensions
{
    public static ModelBuilder ApplyIdempotencyConfiguration(
        this ModelBuilder builder)
    {
        builder.ApplyConfiguration(new IdempotencyRecordConfiguration());
        return builder;
    }
}
