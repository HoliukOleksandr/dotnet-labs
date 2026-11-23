using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

/// <summary>M:N Product &lt;-&gt; Category через явну проміжну сутність із власними полями.</summary>
public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("ProductCategories");
        builder.HasKey(pc => new { pc.ProductId, pc.CategoryId });   // складений ключ
        builder.Property(pc => pc.AddedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(pc => pc.Product).WithMany(p => p.ProductCategories)
               .HasForeignKey(pc => pc.ProductId);
        builder.HasOne(pc => pc.Category).WithMany(c => c.ProductCategories)
               .HasForeignKey(pc => pc.CategoryId);

        builder.HasIndex(pc => new { pc.CategoryId, pc.AddedAt });   // «нещодавно додані товари категорії»
        builder.HasIndex(pc => pc.ProductId)                         // у товару одна основна категорія
               .IsUnique()
               .HasFilter("[IsPrimary] = 1");
    }
}
