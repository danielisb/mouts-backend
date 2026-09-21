using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");

        builder.HasKey(sale => sale.Id);
        builder.Property(sale => sale.Id).ValueGeneratedNever();

        builder.Property(sale => sale.SaleNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(sale => sale.SaleNumber)
            .IsUnique()
            .HasDatabaseName("IX_Sales_SaleNumber");

        builder.Property(sale => sale.SaleDate)
            .HasColumnType("timestamp with time zone");

        builder.Property(sale => sale.CustomerName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(sale => sale.BranchName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(sale => sale.TotalAmount)
            .HasPrecision(18, 2);

        builder.HasMany(sale => sale.Items)
            .WithOne()
            .HasForeignKey(item => item.SaleId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}