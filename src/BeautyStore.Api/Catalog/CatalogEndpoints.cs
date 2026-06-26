using BeautyStore.Api.Catalog.Dtos;
using BeautyStore.Api.Data;
using BeautyStore.Api.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeautyStore.Api.Catalog;

public static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this RouteGroupBuilder group)
    {
        // ── GET /api/catalog/categories ───────────────────────────────────────
        group.MapGet("/categories", async ([FromServices] BeautyStoreDbContext db) =>
        {
            var categories = await db.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new CategoryDto(
                    c.Id,
                    c.Name,
                    c.Slug,
                    c.Description,
                    c.ImageUrl,
                    c.DisplayOrder,
                    c.Products.Count(p => p.IsActive)))
                .ToListAsync();

            return Results.Ok(categories);
        })
        .WithName("GetCategories")
        .WithSummary("Returns all active categories with product counts.");

        // ── GET /api/catalog/products ─────────────────────────────────────────
        group.MapGet("/products", async (
            [FromQuery] int     page      = 1,
            [FromQuery] int     pageSize  = 20,
            [FromQuery] string? category  = null,
            [FromQuery] string? sortBy    = null,
            [FromServices] BeautyStoreDbContext db = default!) =>
        {
            var query = db.Products
                .Where(p => p.IsActive)
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.Category.Slug == category);

            query = sortBy switch
            {
                "price_asc"  => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "rating"     => query.OrderByDescending(p => p.Rating),
                _            => query.OrderByDescending(p => p.IsFeatured).ThenBy(p => p.Id),
            };

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto(
                    p.Id, p.CategoryId, p.Category.Name,
                    p.Name, p.Brand, p.Description,
                    p.Price, p.Rating,
                    p.Stock, p.ImageUrl, p.IsFeatured))
                .ToListAsync();

            return Results.Ok(new PagedResult<ProductDto>(
                items, page, pageSize, total,
                (int)Math.Ceiling(total / (double)pageSize)));
        })
        .WithName("GetProducts")
        .WithSummary("Returns paged, filterable product list.");

        // ── GET /api/catalog/products/{id} ────────────────────────────────────
        group.MapGet("/products/{id:int}", async (
            int id,
            [FromServices] BeautyStoreDbContext db) =>
        {
            var p = await db.Products
                .Include(p => p.Category)
                .Where(p => p.Id == id && p.IsActive)
                .Select(p => new
                {
                    p.Id, p.CategoryId,
                    CategoryName = p.Category.Name,
                    CategorySlug = p.Category.Slug,
                    p.Name, p.Brand, p.Description,
                    p.Price, p.Rating, p.Stock,
                    p.ImageUrl, p.IsFeatured,
                })
                .FirstOrDefaultAsync()
                ?? throw new NotFoundException($"Product {id} not found.");

            var related = await db.Products
                .Where(r => r.CategoryId == p.CategoryId && r.IsActive && r.Id != id)
                .OrderByDescending(r => r.Rating)
                .Take(4)
                .Select(r => new ProductDto(
                    r.Id, r.CategoryId, p.CategoryName,
                    r.Name, r.Brand, r.Description,
                    r.Price, r.Rating,
                    r.Stock, r.ImageUrl, r.IsFeatured))
                .ToListAsync();

            return Results.Ok(new ProductDetailDto(
                p.Id, p.CategoryId, p.CategoryName, p.CategorySlug,
                p.Name, p.Brand, p.Description,
                p.Price, p.Rating, p.Stock, p.ImageUrl, p.IsFeatured,
                related));
        })
        .WithName("GetProductById")
        .WithSummary("Returns full product detail with related products.");

        // ── GET /api/catalog/categories/{slug}/products ───────────────────────
        group.MapGet("/categories/{slug}/products", async (
            string slug,
            [FromServices] BeautyStoreDbContext db) =>
        {
            var category = await db.Categories
                .Where(c => c.Slug == slug && c.IsActive)
                .Select(c => new { c.Id, c.Name, c.Slug })
                .FirstOrDefaultAsync()
                ?? throw new NotFoundException($"Category '{slug}' not found.");

            var products = await db.Products
                .Where(p => p.CategoryId == category.Id && p.IsActive)
                .OrderByDescending(p => p.Rating)
                .Select(p => new ProductDto(
                    p.Id, p.CategoryId, category.Name,
                    p.Name, p.Brand, p.Description,
                    p.Price, p.Rating,
                    p.Stock, p.ImageUrl, p.IsFeatured))
                .ToListAsync();

            return Results.Ok(new { category, products });
        })
        .WithName("GetProductsByCategory")
        .WithSummary("Returns all active products in a category by slug.");

        // ── GET /api/catalog/search ───────────────────────────────────────────
        group.MapGet("/search", async (
            [FromQuery] string? q        = null,
            [FromQuery] string? category = null,
            [FromQuery] decimal minPrice = 0,
            [FromQuery] decimal maxPrice = decimal.MaxValue,
            [FromQuery] float   minRating = 0,
            [FromQuery] string? sortBy   = null,
            [FromQuery] int     page     = 1,
            [FromQuery] int     pageSize = 20,
            [FromServices] BeautyStoreDbContext db = default!) =>
        {
            var query = db.Products
                .Where(p => p.IsActive)
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(p =>
                    p.Name.Contains(q) ||
                    p.Brand.Contains(q) ||
                    (p.Description != null && p.Description.Contains(q)));

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.Category.Slug == category);

            if (minPrice > 0)
                query = query.Where(p => p.Price >= minPrice);

            if (maxPrice < decimal.MaxValue)
                query = query.Where(p => p.Price <= maxPrice);

            if (minRating > 0)
                query = query.Where(p => p.Rating >= minRating);

            query = sortBy switch
            {
                "price_asc"  => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "rating"     => query.OrderByDescending(p => p.Rating),
                _            => query.OrderByDescending(p => p.Id),
            };

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto(
                    p.Id, p.CategoryId, p.Category.Name,
                    p.Name, p.Brand, p.Description,
                    p.Price, p.Rating,
                    p.Stock, p.ImageUrl, p.IsFeatured))
                .ToListAsync();

            return Results.Ok(new PagedResult<ProductDto>(
                items, page, pageSize, total,
                (int)Math.Ceiling(total / (double)pageSize)));
        })
        .WithName("SearchProducts")
        .WithSummary("Search products by name, brand, description with filters.");
    }
}
