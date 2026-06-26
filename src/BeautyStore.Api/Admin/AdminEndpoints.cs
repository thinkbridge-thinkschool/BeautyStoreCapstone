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
            [FromQuery] int      page         = 1,
            [FromQuery] int      pageSize     = 20,
            [FromQuery] bool?    isActive     = null,
            [FromQuery] string?  search       = null,
            [FromQuery] int?     categoryId   = null,
            [FromQuery] bool?    isFeatured   = null,
            [FromQuery] bool?    lowStockOnly = null,
            [FromQuery] decimal? minPrice     = null,
            [FromQuery] decimal? maxPrice     = null,
            [FromQuery] float?   minRating    = null,
            [FromQuery] float?   maxRating    = null,
            [FromQuery] string?  sortBy       = "name",
            [FromQuery] string?  sortDir      = "asc",
            [FromServices] BeautyStoreDbContext db = default!) =>
        {
            var query = db.Products.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.Brand.Contains(search) ||
                    (p.Description != null && p.Description.Contains(search)) ||
                    p.Category.Name.Contains(search));

            if (isActive.HasValue)    query = query.Where(p => p.IsActive   == isActive.Value);
            if (categoryId.HasValue)  query = query.Where(p => p.CategoryId == categoryId.Value);
            if (isFeatured.HasValue)  query = query.Where(p => p.IsFeatured == isFeatured.Value);
            if (lowStockOnly == true) query = query.Where(p => p.Stock < 10);
            if (minPrice.HasValue)    query = query.Where(p => p.Price  >= minPrice.Value);
            if (maxPrice.HasValue)    query = query.Where(p => p.Price  <= maxPrice.Value);
            if (minRating.HasValue)   query = query.Where(p => p.Rating >= minRating.Value);
            if (maxRating.HasValue)   query = query.Where(p => p.Rating <= maxRating.Value);

            query = (sortBy?.ToLowerInvariant(), sortDir?.ToLowerInvariant()) switch
            {
                ("price",     "desc") => query.OrderByDescending(p => p.Price),
                ("price",     _)      => query.OrderBy(p => p.Price),
                ("stock",     "desc") => query.OrderByDescending(p => p.Stock),
                ("stock",     _)      => query.OrderBy(p => p.Stock),
                ("rating",    "desc") => query.OrderByDescending(p => p.Rating),
                ("rating",    _)      => query.OrderBy(p => p.Rating),
                ("createdat", "desc") => query.OrderByDescending(p => p.CreatedAtUtc),
                ("createdat", _)      => query.OrderBy(p => p.CreatedAtUtc),
                ("updatedat", "desc") => query.OrderByDescending(p => p.UpdatedAtUtc),
                ("updatedat", _)      => query.OrderBy(p => p.UpdatedAtUtc),
                ("name",      "desc") => query.OrderByDescending(p => p.Name),
                _                     => query.OrderBy(p => p.Name),
            };

            var total = await query.CountAsync();
            var items = await query
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
        .WithSummary("Returns products with search, filter, sort, and pagination.");

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

        // ── DELETE /api/admin/categories/{id} ────────────────────────────────
        group.MapDelete("/categories/{id:int}", async (
            int id,
            [FromServices] BeautyStoreDbContext db) =>
        {
            var category = await db.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new NotFoundException($"Category {id} not found.");

            var productCount = category.Products.Count;

            if (productCount == 0)
            {
                db.Categories.Remove(category);
                await db.SaveChangesAsync();
                return Results.NoContent();
            }

            if (!category.IsActive)
                throw new ConflictException(
                    $"Category '{category.Name}' is already hidden and still has {productCount} product(s) assigned. Reassign or delete products first.");

            category.IsActive     = false;
            category.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                softDeleted = true,
                message     = $"Category '{category.Name}' hidden — {productCount} product(s) still assigned to it.",
            });
        })
        .WithName("AdminDeleteCategory")
        .WithSummary("Hard-deletes an empty category; soft-deletes (hides) one that still has products.");

        // ── GET /api/admin/analytics ──────────────────────────────────────────
        group.MapGet("/analytics", async ([FromServices] BeautyStoreDbContext db) =>
        {
            var now          = DateTime.UtcNow;
            var today        = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
            var tomorrow     = today.AddDays(1);
            var monthStart   = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var thirtyDaysAgo = today.AddDays(-29);

            // ── Revenue & order counts ─────────────────────────────────────────
            var revenueToday = await db.Orders
                .AsNoTracking()
                .Where(o => o.CreatedAtUtc >= today && o.CreatedAtUtc < tomorrow)
                .SumAsync(o => (decimal?)o.TotalPrice) ?? 0m;

            var ordersToday = await db.Orders
                .AsNoTracking()
                .CountAsync(o => o.CreatedAtUtc >= today && o.CreatedAtUtc < tomorrow);

            var revenueThisMonth = await db.Orders
                .AsNoTracking()
                .Where(o => o.CreatedAtUtc >= monthStart)
                .SumAsync(o => (decimal?)o.TotalPrice) ?? 0m;

            var ordersThisMonth = await db.Orders
                .AsNoTracking()
                .CountAsync(o => o.CreatedAtUtc >= monthStart);

            // ── Catalog counts ─────────────────────────────────────────────────
            var customerCount = await db.Users.AsNoTracking().CountAsync();
            var productCount  = await db.Products.AsNoTracking().CountAsync(p => p.IsActive);
            var categoryCount = await db.Categories.AsNoTracking().CountAsync(c => c.IsActive);
            var lowStockCount = await db.Products.AsNoTracking()
                                         .CountAsync(p => p.IsActive && p.Stock < 10);

            // ── Top 5 products by revenue ──────────────────────────────────────
            var topProducts = await db.Orders
                .AsNoTracking()
                .GroupBy(o => new { o.ProductId, o.ProductName })
                .Select(g => new TopProductDto(
                    g.Key.ProductId,
                    g.Key.ProductName,
                    g.Sum(o => o.TotalPrice),
                    g.Sum(o => o.Quantity)))
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToListAsync();

            // ── Top 5 categories by revenue (Orders → Products → Categories) ───
            var topCategories = await (
                from o in db.Orders.AsNoTracking()
                join p in db.Products.AsNoTracking() on o.ProductId equals p.Id
                join c in db.Categories.AsNoTracking() on p.CategoryId equals c.Id
                group new { o.TotalPrice } by c.Name into g
                select new TopCategoryDto(g.Key, g.Sum(x => x.TotalPrice), g.Count())
            ).OrderByDescending(x => x.Revenue)
             .Take(5)
             .ToListAsync();

            // ── Revenue trend: last 30 days grouped by date ────────────────────
            var rawTrend = await db.Orders
                .AsNoTracking()
                .Where(o => o.CreatedAtUtc >= thirtyDaysAgo)
                .GroupBy(o => o.CreatedAtUtc.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalPrice), OrderCount = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var revenueTrend = rawTrend
                .Select(x => new RevenueTrendDto(DateOnly.FromDateTime(x.Date), x.Revenue, x.OrderCount))
                .ToList();

            return Results.Ok(new AnalyticsDto(
                revenueToday, revenueThisMonth,
                ordersToday,  ordersThisMonth,
                customerCount, productCount, categoryCount, lowStockCount,
                topProducts, topCategories, revenueTrend));
        })
        .WithName("AdminGetAnalytics")
        .WithSummary("Returns site-wide analytics: revenue, orders, catalog counts, top products/categories, 30-day trend.");

        // ── GET /api/admin/inventory ──────────────────────────────────────────
        group.MapGet("/inventory", async (
            [FromQuery] int     page         = 1,
            [FromQuery] int     pageSize     = 20,
            [FromQuery] string? search       = null,
            [FromQuery] string? sortBy       = "name",
            [FromQuery] string? sortDir      = "asc",
            [FromQuery] bool?   lowStockOnly = null,
            [FromQuery] bool?   isActive     = null,
            [FromServices] BeautyStoreDbContext db = default!) =>
        {
            var query = db.Products.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.Brand.Contains(search) ||
                    p.Category.Name.Contains(search));

            if (isActive.HasValue)
                query = query.Where(p => p.IsActive == isActive.Value);

            if (lowStockOnly == true)
                query = query.Where(p => p.Stock < 10);

            query = (sortBy?.ToLowerInvariant(), sortDir?.ToLowerInvariant()) switch
            {
                ("stock",    "desc") => query.OrderByDescending(p => p.Stock),
                ("stock",    _)      => query.OrderBy(p => p.Stock),
                ("price",    "desc") => query.OrderByDescending(p => p.Price),
                ("price",    _)      => query.OrderBy(p => p.Price),
                ("category", "desc") => query.OrderByDescending(p => p.Category.Name),
                ("category", _)      => query.OrderBy(p => p.Category.Name),
                ("name",     "desc") => query.OrderByDescending(p => p.Name),
                _                    => query.OrderBy(p => p.Name),
            };

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new InventoryItemDto(
                    p.Id, p.Name, p.Category.Name,
                    p.Stock, p.Price, p.IsActive,
                    p.Stock < 10))
                .ToListAsync();

            return Results.Ok(new PagedResult<InventoryItemDto>(
                items, page, pageSize, total,
                (int)Math.Ceiling(total / (double)pageSize)));
        })
        .WithName("AdminGetInventory")
        .WithSummary("Returns products with inventory data, supporting search, sort, and filters.");

        // ── PUT /api/admin/inventory/{productId} ──────────────────────────────
        group.MapPut("/inventory/{productId:int}", async (
            int productId,
            UpdateStockRequest req,
            [FromServices] BeautyStoreDbContext db) =>
        {
            if (req.Stock < 0)
                throw new ValidationException("Stock cannot be negative.",
                    new Dictionary<string, string[]> { ["stock"] = ["Must be >= 0."] });

            var product = await db.Products.FindAsync(productId)
                ?? throw new NotFoundException($"Product {productId} not found.");

            product.Stock        = req.Stock;
            product.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("AdminUpdateStock")
        .WithSummary("Updates the stock level of a product.");

        // ── GET /api/admin/orders ─────────────────────────────────────────────
        group.MapGet("/orders", async (
            [FromQuery] int       page      = 1,
            [FromQuery] int       pageSize  = 20,
            [FromQuery] string?   status    = null,
            [FromQuery] string?   search    = null,
            [FromQuery] DateTime? dateFrom  = null,
            [FromQuery] DateTime? dateTo    = null,
            [FromQuery] decimal?  minAmount = null,
            [FromQuery] decimal?  maxAmount = null,
            [FromQuery] string?   sortBy    = "date",
            [FromQuery] string?   sortDir   = "desc",
            [FromServices] BeautyStoreDbContext db = default!) =>
        {
            var joined = from o in db.Orders.AsNoTracking()
                         join u in db.Users.AsNoTracking() on o.UserId equals u.Id into uj
                         from u in uj.DefaultIfEmpty()
                         select new
                         {
                             o,
                             UserEmail = (string?)u.Email,
                             UserName  = (string?)u.UserName,
                             FullName  = (string?)u.FullName,
                         };

            if (!string.IsNullOrWhiteSpace(status))
                joined = joined.Where(x => x.o.Status == status);

            if (!string.IsNullOrWhiteSpace(search))
            {
                if (int.TryParse(search, out var searchId))
                    joined = joined.Where(x => x.o.Id == searchId);
                else
                    joined = joined.Where(x =>
                        (x.UserEmail != null && x.UserEmail.Contains(search)) ||
                        (x.UserName  != null && x.UserName.Contains(search))  ||
                        (x.FullName  != null && x.FullName.Contains(search)));
            }

            if (dateFrom.HasValue)  joined = joined.Where(x => x.o.CreatedAtUtc >= dateFrom.Value);
            if (dateTo.HasValue)    joined = joined.Where(x => x.o.CreatedAtUtc <= dateTo.Value.AddDays(1));
            if (minAmount.HasValue) joined = joined.Where(x => x.o.TotalPrice   >= minAmount.Value);
            if (maxAmount.HasValue) joined = joined.Where(x => x.o.TotalPrice   <= maxAmount.Value);

            joined = (sortBy?.ToLowerInvariant(), sortDir?.ToLowerInvariant()) switch
            {
                ("customer", "asc")  => joined.OrderBy(x => x.UserEmail),
                ("customer", _)      => joined.OrderByDescending(x => x.UserEmail),
                ("total",    "asc")  => joined.OrderBy(x => x.o.TotalPrice),
                ("total",    _)      => joined.OrderByDescending(x => x.o.TotalPrice),
                ("status",   "asc")  => joined.OrderBy(x => x.o.Status),
                ("status",   _)      => joined.OrderByDescending(x => x.o.Status),
                ("date",     "asc")  => joined.OrderBy(x => x.o.CreatedAtUtc),
                _                    => joined.OrderByDescending(x => x.o.CreatedAtUtc),
            };

            var total = await joined.CountAsync();
            var items = await joined
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AdminOrderDto(
                    x.o.Id, x.o.UserId, x.o.ProductName,
                    x.o.Quantity, x.o.TotalPrice, x.o.Status, x.o.CreatedAtUtc,
                    x.UserEmail ?? "", x.UserName ?? ""))
                .ToListAsync();

            return Results.Ok(new PagedResult<AdminOrderDto>(
                items, page, pageSize, total,
                (int)Math.Ceiling(total / (double)pageSize)));
        })
        .WithName("AdminGetOrders")
        .WithSummary("Returns orders with search, filter, sort, and pagination.");

        // ── PUT /api/admin/orders/{id}/status ────────────────────────────────
        group.MapPut("/orders/{id:int}/status", async (
            int id,
            UpdateOrderStatusRequest req,
            [FromServices] BeautyStoreDbContext db) =>
        {
            var allowed = new[] { "Created", "Confirmed", "Shipped", "Delivered", "Cancelled" };
            if (!allowed.Contains(req.Status, StringComparer.OrdinalIgnoreCase))
                return Results.Problem(
                    title:      "Invalid order status",
                    detail:     $"Allowed values: {string.Join(", ", allowed)}",
                    statusCode: StatusCodes.Status400BadRequest);

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
            [FromQuery] int     page     = 1,
            [FromQuery] int     pageSize = 20,
            [FromQuery] string? search   = null,
            [FromQuery] string? role     = null,
            [FromQuery] string? sortBy   = "email",
            [FromQuery] string? sortDir  = "asc",
            [FromServices] UserManager<ApplicationUser> userManager = default!,
            [FromServices] BeautyStoreDbContext db = default!) =>
        {
            var query = userManager.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u =>
                    (u.Email    != null && u.Email.Contains(search))    ||
                    (u.UserName != null && u.UserName.Contains(search)) ||
                    u.FullName.Contains(search));

            if (!string.IsNullOrWhiteSpace(role))
            {
                var roleId = await db.Roles
                    .Where(r => r.Name == role)
                    .Select(r => r.Id)
                    .FirstOrDefaultAsync();
                if (roleId == null)
                    return Results.Ok(new PagedResult<AdminUserDto>([], page, pageSize, 0, 0));
                query = query.Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId));
            }

            query = (sortBy?.ToLowerInvariant(), sortDir?.ToLowerInvariant()) switch
            {
                ("username",  "desc") => query.OrderByDescending(u => u.UserName),
                ("username",  _)      => query.OrderBy(u => u.UserName),
                ("createdat", "desc") => query.OrderByDescending(u => u.CreatedAt),
                ("createdat", _)      => query.OrderBy(u => u.CreatedAt),
                ("email",     "desc") => query.OrderByDescending(u => u.Email),
                _                     => query.OrderBy(u => u.Email),
            };

            var total     = await query.CountAsync();
            var pageUsers = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new List<AdminUserDto>(pageUsers.Count);
            foreach (var user in pageUsers)
            {
                var roles = await userManager.GetRolesAsync(user);
                result.Add(new AdminUserDto(
                    user.Id, user.Email!, user.UserName!, roles.ToList(),
                    user.FullName, user.CreatedAt));
            }

            return Results.Ok(new PagedResult<AdminUserDto>(
                result, page, pageSize, total,
                (int)Math.Ceiling(total / (double)pageSize)));
        })
        .WithName("AdminGetUsers")
        .WithSummary("Returns users with search, role filter, sort, and pagination.");

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

        // ── GET /api/admin/settings ───────────────────────────────────────────
        group.MapGet("/settings", async (
            HttpContext                          ctx,
            [FromServices] IWebHostEnvironment  env    = default!,
            [FromServices] IConfiguration       config = default!,
            [FromServices] BeautyStoreDbContext db     = default!) =>
        {
            // ── Application info ────────────────────────────────────────────
            var version     = typeof(AdminEndpoints).Assembly.GetName().Version?.ToString() ?? "1.0.0";
            var apiBaseUrl  = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
            var assemblyLoc = typeof(AdminEndpoints).Assembly.Location;
            var buildTime   = !string.IsNullOrEmpty(assemblyLoc) && File.Exists(assemblyLoc)
                ? File.GetLastWriteTimeUtc(assemblyLoc)
                : System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();

            var application = new ApplicationInfoDto(
                "BeautyStore API", version, env.EnvironmentName, apiBaseUrl, buildTime);

            // ── Azure service status (config-based) ─────────────────────────
            ServiceStatusDto ConfigStatus(string key)
            {
                var value = config[key];
                return !string.IsNullOrWhiteSpace(value)
                    ? new ServiceStatusDto("Configured",     "Connection string present")
                    : new ServiceStatusDto("Not configured", "No connection string found");
            }

            var azure = new AzureStatusDto(
                SqlDatabase:         ConfigStatus("ConnectionStrings:BeautyStoreDb"),
                BlobStorage:         ConfigStatus("Storage:AccountName"),
                ServiceBus:          ConfigStatus("ServiceBus:Namespace"),
                ApplicationInsights: ConfigStatus("APPLICATIONINSIGHTS_CONNECTION_STRING"),
                OpenTelemetry:       ConfigStatus("APPLICATIONINSIGHTS_CONNECTION_STRING"));

            // ── Security flags (always on in this app) ──────────────────────
            var security = new SecurityInfoDto(
                JwtEnabled:                 true,
                IdentityEnabled:            true,
                RoleAuthorizationEnabled:   true,
                ExceptionMiddlewareEnabled: true,
                ProblemDetailsEnabled:      true);

            // ── System health ───────────────────────────────────────────────
            var proc          = System.Diagnostics.Process.GetCurrentProcess();
            var processUptime = DateTime.UtcNow - proc.StartTime.ToUniversalTime();
            var serverUptime  = $"{(int)processUptime.TotalHours}h {processUptime.Minutes}m {processUptime.Seconds}s";

            var dbReachable  = false;
            int productCount = 0, categoryCount = 0, userCount = 0;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                dbReachable   = await db.Database.CanConnectAsync(cts.Token);
                if (dbReachable)
                {
                    productCount  = await db.Products.AsNoTracking().CountAsync();
                    categoryCount = await db.Categories.AsNoTracking().CountAsync();
                    userCount     = await db.Users.AsNoTracking().CountAsync();
                }
            }
            catch { /* DB temporarily unreachable */ }

            var system = new SystemHealthDto(
                HealthEndpoint:    "/health",
                CurrentTimeUtc:    DateTime.UtcNow,
                ServerUptime:      serverUptime,
                DatabaseReachable: dbReachable,
                ProductCount:      productCount,
                CategoryCount:     categoryCount,
                UserCount:         userCount);

            // ── Deployment info ─────────────────────────────────────────────
            var deployment = new DeploymentInfoDto(
                ContainerAppName:      Environment.GetEnvironmentVariable("CONTAINER_APP_NAME")     ?? "localhost",
                ContainerRevision:     Environment.GetEnvironmentVariable("CONTAINER_APP_REVISION") ?? "N/A",
                DeploymentEnvironment: env.EnvironmentName,
                GitCommit:             Environment.GetEnvironmentVariable("GIT_COMMIT")             ?? "N/A");

            return Results.Ok(new SettingsDto(application, azure, security, system, deployment));
        })
        .WithName("AdminGetSettings")
        .WithSummary("Returns application settings, Azure service status, and system health.");
    }
}
