using BeautyStore.Api.Admin.Dtos;
using BeautyStore.Api.Auth;
using BeautyStore.Api.Catalog;
using BeautyStore.Api.Catalog.Dtos;
using BeautyStore.Api.Data;
using BeautyStore.Api.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace BeautyStore.Api.Admin;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this RouteGroupBuilder group)
    {
        // ── GET /api/admin/dashboard ──────────────────────────────────────────
        group.MapGet("/dashboard", async ([FromServices] BeautyStoreDbContext db) =>
        {
            var totalOrders   = await db.Orders.CountAsync();
            var totalRevenue  = await db.Orders.SumAsync(o => (decimal?)o.TotalPrice) ?? 0m;
            var totalProducts = await db.Products.CountAsync(p => p.IsActive);
            var totalUsers    = await db.Users.CountAsync();

            var recentOrders = await db.Orders
                .OrderByDescending(o => o.CreatedAtUtc)
                .Take(5)
                .Select(o => new AdminOrderDto(
                    o.Id, o.UserId, o.ProductName,
                    o.Quantity, o.TotalPrice, o.Status, o.CreatedAtUtc))
                .ToListAsync();

            return Results.Ok(new DashboardDto(
                totalOrders, totalRevenue, totalProducts, totalUsers, recentOrders));
        })
        .WithName("AdminDashboard")
        .WithSummary("Returns site-wide stats and 5 most recent orders.");

        // ── GET /api/admin/products ───────────────────────────────────────────
        group.MapGet("/products", async (
            [FromQuery] int   page     = 1,
            [FromQuery] int   pageSize = 20,
            [FromQuery] bool? isActive = null,
            [FromServices] BeautyStoreDbContext db = default!) =>
        {
            var query = db.Products.Include(p => p.Category).AsQueryable();
            if (isActive.HasValue)
                query = query.Where(p => p.IsActive == isActive.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new AdminProductDto(
                    p.Id, p.CategoryId, p.Category.Name,
                    p.Name, p.Brand, p.Description,
                    p.Price, p.Rating, p.Stock,
                    p.ImageUrl, p.IsFeatured, p.IsActive,
                    p.CreatedAtUtc, p.UpdatedAtUtc))
                .ToListAsync();

            return Results.Ok(new PagedResult<AdminProductDto>(
                items, page, pageSize, total,
                (int)Math.Ceiling(total / (double)pageSize)));
        })
        .WithName("AdminGetProducts")
        .WithSummary("Returns all products (including inactive), paged.");

        // ── POST /api/admin/products ──────────────────────────────────────────
        group.MapPost("/products", async (
            CreateProductRequest req,
            [FromServices] BeautyStoreDbContext db) =>
        {
            var categoryExists = await db.Categories.AnyAsync(c => c.Id == req.CategoryId && c.IsActive);
            if (!categoryExists)
                throw new NotFoundException($"Category {req.CategoryId} not found.");

            var product = new Product
            {
                CategoryId   = req.CategoryId,
                Name         = req.Name,
                Brand        = req.Brand,
                Description  = req.Description,
                Price        = req.Price,
                Rating       = req.Rating,
                Stock        = req.Stock,
                ImageUrl     = req.ImageUrl,
                IsFeatured   = req.IsFeatured,
                IsActive     = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return Results.Created($"/api/admin/products/{product.Id}", new { product.Id });
        })
        .WithName("AdminCreateProduct")
        .WithSummary("Creates a new product.");

        // ── PUT /api/admin/products/{id} ──────────────────────────────────────
        group.MapPut("/products/{id:int}", async (
            int id,
            UpdateProductRequest req,
            [FromServices] BeautyStoreDbContext db) =>
        {
            var product = await db.Products.FindAsync(id)
                ?? throw new NotFoundException($"Product {id} not found.");

            product.CategoryId   = req.CategoryId;
            product.Name         = req.Name;
            product.Brand        = req.Brand;
            product.Description  = req.Description;
            product.Price        = req.Price;
            product.Rating       = req.Rating;
            product.Stock        = req.Stock;
            product.ImageUrl     = req.ImageUrl;
            product.IsFeatured   = req.IsFeatured;
            product.IsActive     = req.IsActive;
            product.UpdatedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("AdminUpdateProduct")
        .WithSummary("Updates an existing product.");

        // ── DELETE /api/admin/products/{id} — soft delete ─────────────────────
        group.MapDelete("/products/{id:int}", async (
            int id,
            [FromServices] BeautyStoreDbContext db) =>
        {
            var product = await db.Products.FindAsync(id)
                ?? throw new NotFoundException($"Product {id} not found.");
            product.IsActive     = false;
            product.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("AdminDeleteProduct")
        .WithSummary("Soft-deletes a product (sets IsActive = false).");

        // ── GET /api/admin/categories ─────────────────────────────────────────
        group.MapGet("/categories", async ([FromServices] BeautyStoreDbContext db) =>
        {
            var categories = await db.Categories
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new AdminCategoryDto(
                    c.Id, c.Name, c.Slug, c.Description, c.ImageUrl,
                    c.DisplayOrder, c.IsActive,
                    c.Products.Count(p => p.IsActive)))
                .ToListAsync();
            return Results.Ok(categories);
        })
        .WithName("AdminGetCategories")
        .WithSummary("Returns all categories (including inactive) with product counts.");

        // ── POST /api/admin/categories ────────────────────────────────────────
        group.MapPost("/categories", async (
            CreateCategoryRequest req,
            [FromServices] BeautyStoreDbContext db) =>
        {
            var slugExists = await db.Categories.AnyAsync(c => c.Slug == req.Slug);
            if (slugExists)
                throw new ValidationException("Slug already exists.",
                    new Dictionary<string, string[]> { ["slug"] = ["Must be unique."] });

            var category = new Category
            {
                Name         = req.Name,
                Slug         = req.Slug,
                Description  = req.Description,
                ImageUrl     = req.ImageUrl,
                DisplayOrder = req.DisplayOrder,
                IsActive     = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            return Results.Created($"/api/admin/categories/{category.Id}", new { category.Id });
        })
        .WithName("AdminCreateCategory")
        .WithSummary("Creates a new category.");

        // ── PUT /api/admin/categories/{id} ────────────────────────────────────
        group.MapPut("/categories/{id:int}", async (
            int id,
            UpdateCategoryRequest req,
            [FromServices] BeautyStoreDbContext db) =>
        {
            var category = await db.Categories.FindAsync(id)
                ?? throw new NotFoundException($"Category {id} not found.");

            var slugTaken = await db.Categories.AnyAsync(c => c.Slug == req.Slug && c.Id != id);
            if (slugTaken)
                throw new ValidationException("Slug already in use.",
                    new Dictionary<string, string[]> { ["slug"] = ["Must be unique."] });

            category.Name         = req.Name;
            category.Slug         = req.Slug;
            category.Description  = req.Description;
            category.ImageUrl     = req.ImageUrl;
            category.DisplayOrder = req.DisplayOrder;
            category.IsActive     = req.IsActive;
            category.UpdatedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("AdminUpdateCategory")
        .WithSummary("Updates an existing category.");

        // ── GET /api/admin/orders ─────────────────────────────────────────────
        group.MapGet("/orders", async (
            [FromQuery] int     page     = 1,
            [FromQuery] int     pageSize = 20,
            [FromQuery] string? status   = null,
            [FromServices] BeautyStoreDbContext db = default!) =>
        {
            var query = db.Orders.AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.Status == status);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(o => o.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new AdminOrderDto(
                    o.Id, o.UserId, o.ProductName,
                    o.Quantity, o.TotalPrice, o.Status, o.CreatedAtUtc))
                .ToListAsync();

            return Results.Ok(new PagedResult<AdminOrderDto>(
                items, page, pageSize, total,
                (int)Math.Ceiling(total / (double)pageSize)));
        })
        .WithName("AdminGetOrders")
        .WithSummary("Returns all orders, paged, optionally filtered by status.");

        // ── PUT /api/admin/orders/{id}/status ────────────────────────────────
        group.MapPut("/orders/{id:int}/status", async (
            int id,
            UpdateOrderStatusRequest req,
            [FromServices] BeautyStoreDbContext db) =>
        {
            var order = await db.Orders.FindAsync(id)
                ?? throw new NotFoundException($"Order {id} not found.");
            order.Status = req.Status;
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("AdminUpdateOrderStatus")
        .WithSummary("Updates the status of an order.");

        // ── GET /api/admin/users ──────────────────────────────────────────────
        group.MapGet("/users", async (
            [FromServices] UserManager<ApplicationUser> userManager) =>
        {
            var users = await userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            var result = new List<AdminUserDto>(users.Count);
            foreach (var user in users)
            {
                var roles = await userManager.GetRolesAsync(user);
                result.Add(new AdminUserDto(user.Id, user.Email!, user.UserName!, roles.ToList()));
            }
            return Results.Ok(result);
        })
        .WithName("AdminGetUsers")
        .WithSummary("Returns all users with their assigned roles.");

        // ── POST /api/admin/users/{id}/roles ─────────────────────────────────
        group.MapPost("/users/{id}/roles", async (
            string id,
            AssignRoleRequest req,
            [FromServices] UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(id)
                ?? throw new NotFoundException($"User {id} not found.");

            var result = await userManager.AddToRoleAsync(user, req.Role);
            if (!result.Succeeded)
                throw new ValidationException("Failed to assign role.",
                    result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));

            return Results.NoContent();
        })
        .WithName("AdminAssignRole")
        .WithSummary("Assigns a role to a user.");
    }
}
