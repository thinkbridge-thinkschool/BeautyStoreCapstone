using BeautyStore.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace BeautyStore.Api.Catalog;

public static class ProductSeeder
{
    public static async Task SeedAsync(BeautyStoreDbContext db)
    {
        if (await db.Categories.AnyAsync()) return;

        // ── Categories ────────────────────────────────────────────────────────
        // Inserted in order; IDENTITY starts at 1 on a fresh table so IDs will
        // be 1–6, matching the product IDs already referenced by existing Orders.
        var categories = new List<Category>
        {
            new() { Name = "Makeup",     Slug = "makeup",     DisplayOrder = 1, IsActive = true },
            new() { Name = "Skincare",   Slug = "skincare",   DisplayOrder = 2, IsActive = true },
            new() { Name = "Fragrance",  Slug = "fragrance",  DisplayOrder = 3, IsActive = true },
            new() { Name = "Haircare",   Slug = "haircare",   DisplayOrder = 4, IsActive = true },
            new() { Name = "Tools",      Slug = "tools",      DisplayOrder = 5, IsActive = true },
            new() { Name = "Wellness",   Slug = "wellness",   DisplayOrder = 6, IsActive = true },
        };

        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();

        // EF populates category.Id after SaveChanges — use them for FK references.
        var makeup   = categories[0].Id;
        var skincare = categories[1].Id;

        // ── Products ──────────────────────────────────────────────────────────
        // Names, brands, and prices match the previous hardcoded catalog exactly
        // so existing order records (ProductName, UnitPrice) remain consistent.
        var products = new List<Product>
        {
            new()
            {
                CategoryId  = makeup,
                Name        = "Pro Filt'r Soft Matte Foundation",
                Brand       = "Fenty Beauty",
                Description = "A lightweight, medium-to-full coverage foundation that controls shine and lasts all day. Available in 50 shades.",
                Price       = 3800m,
                Rating      = 4.8f,
                Stock       = 50,
                ImageUrl    = "/images/product-1.jpg",
                IsFeatured  = true,
                IsActive    = true,
            },
            new()
            {
                CategoryId  = makeup,
                Name        = "Pillow Talk Matte Revolution Lipstick",
                Brand       = "Charlotte Tilbury",
                Description = "The iconic nude-pink lipstick loved worldwide. Long-wearing matte formula with a comfortable velvet finish.",
                Price       = 2850m,
                Rating      = 4.9f,
                Stock       = 75,
                ImageUrl    = "/images/product-2.jpg",
                IsFeatured  = true,
                IsActive    = true,
            },
            new()
            {
                CategoryId  = makeup,
                Name        = "Orgasm Blush Powder",
                Brand       = "NARS Cosmetics",
                Description = "A peachy-pink blush with golden shimmer that flatters every skin tone. Silky, long-lasting formula.",
                Price       = 2200m,
                Rating      = 4.7f,
                Stock       = 60,
                ImageUrl    = "/images/product-3.jpg",
                IsFeatured  = true,
                IsActive    = true,
            },
            new()
            {
                CategoryId  = skincare,
                Name        = "Protini Polypeptide Moisturiser",
                Brand       = "Drunk Elephant",
                Description = "A protein-rich moisturiser with signal peptides, amino acids, and pygmy waterlily that visibly firms and improves skin texture.",
                Price       = 5600m,
                Rating      = 4.6f,
                Stock       = 40,
                ImageUrl    = "/images/product-4.jpg",
                IsFeatured  = true,
                IsActive    = true,
            },
            new()
            {
                CategoryId  = skincare,
                Name        = "Facial Treatment Essence",
                Brand       = "SK-II",
                Description = "The legendary essence powered by PITERA™. Clinically proven to improve all 5 skin quality dimensions in 4 weeks.",
                Price       = 12500m,
                Rating      = 4.8f,
                Stock       = 30,
                ImageUrl    = "/images/product-5.jpg",
                IsFeatured  = true,
                IsActive    = true,
            },
            new()
            {
                CategoryId  = makeup,
                Name        = "Rose Gold Eyeshadow Palette",
                Brand       = "Huda Beauty",
                Description = "18 stunning rose gold and metallic shades ranging from satin to glitter. Highly pigmented, blendable formula.",
                Price       = 4800m,
                Rating      = 4.7f,
                Stock       = 45,
                ImageUrl    = "/images/product-6.jpg",
                IsFeatured  = true,
                IsActive    = true,
            },
        };

        db.Products.AddRange(products);
        await db.SaveChangesAsync();
    }
}
