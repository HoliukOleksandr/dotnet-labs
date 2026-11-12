using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Seeding;

/// <summary>
/// Програмний сідінг після MigrateAsync(). Ідемпотентний: якщо товари вже є — нічого не робить.
/// Довідник категорій (HasData) уже створений міграцією, тому FK-цілісність дотримана.
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(CatalogDbContext db, CancellationToken ct = default)
    {
        if (await db.Products.AnyAsync(ct)) return;

        var categories = await db.Categories.ToDictionaryAsync(c => c.Slug, ct);

        // Назви й ціни збігаються з локальною копією довідника в OrdersDb (db/p1/seed.sql)
        var laptop = new Product("Ноутбук X15", "NB-X15", 32999.00m);
        laptop.AddStock(10);
        laptop.SetDetail(new ProductDetail("15.6\" IPS, 16 ГБ RAM, 512 ГБ SSD", "TechCorp", 1800, 24));
        laptop.AddImage("/img/nb-x15-1.jpg", 0, true);
        laptop.AddImage("/img/nb-x15-2.jpg", 1);
        laptop.AssignCategory(categories["computers"], isPrimary: true);
        laptop.AssignCategory(categories["electronics"]);

        var mouse = new Product("Бездротова миша M200", "MS-M200", 799.00m);
        mouse.AddStock(50);
        mouse.SetDetail(new ProductDetail("Бездротова миша 2.4 ГГц", "TechCorp", 90));
        mouse.AddImage("/img/ms-m200-1.jpg", 0, true);
        mouse.AssignCategory(categories["peripherals"], isPrimary: true);
        mouse.AssignCategory(categories["computers"]);

        var keyboard = new Product("Клавіатура K550", "KB-K550", 1499.00m);
        keyboard.AddStock(30);
        keyboard.SetDetail(new ProductDetail("Механічна клавіатура", "KeyWorks", 850));
        keyboard.AddImage("/img/kb-k550-1.jpg", 0, true);
        keyboard.AssignCategory(categories["peripherals"], isPrimary: true);

        var monitor = new Product("Монітор 27\" QHD", "MN-27Q", 8999.00m);
        monitor.AddStock(15);
        monitor.SetDetail(new ProductDetail("27\" QHD IPS, 75 Гц", "ViewMax", 5200, 36));
        monitor.AddImage("/img/mn-27q-1.jpg", 0, true);
        monitor.AssignCategory(categories["peripherals"], isPrimary: true);
        monitor.AssignCategory(categories["electronics"]);

        var headphones = new Product("Навушники H9", "HP-H9", 2499.00m);
        headphones.AddStock(25);
        headphones.SetDetail(new ProductDetail("Накладні навушники з Bluetooth", "SoundLab", 280));
        headphones.AddImage("/img/hp-h9-1.jpg", 0, true);
        headphones.AssignCategory(categories["audio"], isPrimary: true);
        headphones.AssignCategory(categories["electronics"]);

        db.Products.AddRange(laptop, mouse, keyboard, monitor, headphones);
        await db.SaveChangesAsync(ct);
    }
}
