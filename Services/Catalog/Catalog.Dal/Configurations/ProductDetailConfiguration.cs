using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class ProductDetailConfiguration : IEntityTypeConfiguration<ProductDetail>
{
    public void Configure(EntityTypeBuilder<ProductDetail> builder)
    {
        builder.ToTable("ProductDetails", t =>
        {
            t.HasCheckConstraint("CK_ProductDetails_Weight", "[WeightGrams] IS NULL OR [WeightGrams] > 0");
            t.HasCheckConstraint("CK_ProductDetails_Warranty", "[WarrantyMonths] >= 0");
        });
        builder.HasKey(d => d.ProductId);                            // PK є водночас FK (1:1)
        builder.Property(d => d.ProductId).ValueGeneratedNever();
        builder.Property(d => d.Description).IsRequired().HasMaxLength(2000);
        builder.Property(d => d.Manufacturer).HasMaxLength(150);
    }
}
