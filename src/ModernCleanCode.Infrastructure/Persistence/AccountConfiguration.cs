using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModernCleanCode.Domain;

namespace ModernCleanCode.Infrastructure.Persistence;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Balance)
            .HasConversion(m => m.Amount, d => Money.Eur(d))
            .HasColumnType("decimal(19,4)")
            .IsRequired();
    }
}
