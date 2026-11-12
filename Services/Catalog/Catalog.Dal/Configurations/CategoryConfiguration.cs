using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(120);
        builder.HasIndex(c => c.Name).IsUnique();
        builder.HasIndex(c => c.Slug).IsUnique();

        // статичний (модельний) сідінг довідника з фіксованими Id: потрапляє в міграцію
        builder.HasData(
            new { Id = 1, Name = "Електроніка", Slug = "electronics" },
            new { Id = 2, Name = "Комп'ютери",  Slug = "computers" },
            new { Id = 3, Name = "Периферія",   Slug = "peripherals" },
            new { Id = 4, Name = "Аудіо",       Slug = "audio" });
    }
}
