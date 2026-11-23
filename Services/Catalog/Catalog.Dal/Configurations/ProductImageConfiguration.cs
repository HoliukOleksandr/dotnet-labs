using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Url).IsRequired().HasMaxLength(500);

        builder.HasIndex(i => new { i.ProductId, i.SortOrder });     // картинки товару у правильному порядку
        builder.HasIndex(i => i.ProductId)                           // у товару лише одна головна картинка
               .IsUnique()
               .HasFilter("[IsMain] = 1");
    }
}
