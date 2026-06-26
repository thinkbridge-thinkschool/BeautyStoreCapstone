namespace BeautyStore.IntegrationTests;

[Collection("Integration")]
public sealed class CatalogEndpointsTests(BeautyStoreWebApplicationFactory factory)
    : IClassFixture<BeautyStoreWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // ── GET /api/catalog/categories ───────────────────────────────────────────

    [Fact]
    public async Task GetCategories_Returns200WithSeededCategories()
    {
        var response = await _client.GetAsync("/api/catalog/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetCategories_IsPublic_NoAuthRequired()
    {
        // Catalog is intentionally unauthenticated
        var response = await _client.GetAsync("/api/catalog/categories");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCategories_EachItemHasRequiredFields()
    {
        var response = await _client.GetAsync("/api/catalog/categories");
        var json     = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        foreach (var category in doc.RootElement.EnumerateArray())
        {
            category.TryGetProperty("id",   out _).Should().BeTrue();
            category.TryGetProperty("name", out _).Should().BeTrue();
            category.TryGetProperty("slug", out _).Should().BeTrue();
        }
    }

    [Fact]
    public async Task GetCategories_ContainsMakeupCategory()
    {
        var response = await _client.GetAsync("/api/catalog/categories");
        var json     = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var names = doc.RootElement.EnumerateArray()
            .Select(c => c.GetProperty("name").GetString())
            .ToList();

        names.Should().Contain("Makeup");
    }

    // ── GET /api/catalog/products ─────────────────────────────────────────────

    [Fact]
    public async Task GetProducts_Returns200WithPagedResult()
    {
        var response = await _client.GetAsync("/api/catalog/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("items",      out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("totalCount", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("page",       out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetProducts_DefaultPage_ContainsSeededProducts()
    {
        var response = await _client.GetAsync("/api/catalog/products");
        var json     = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var itemCount = doc.RootElement.GetProperty("items").GetArrayLength();
        itemCount.Should().BeGreaterThan(0, "the seeder adds 6 products");
    }

    [Fact]
    public async Task GetProducts_EachItemHasRequiredFields()
    {
        var response = await _client.GetAsync("/api/catalog/products");
        var json     = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        foreach (var product in doc.RootElement.GetProperty("items").EnumerateArray())
        {
            product.TryGetProperty("id",           out _).Should().BeTrue();
            product.TryGetProperty("name",         out _).Should().BeTrue();
            product.TryGetProperty("brand",        out _).Should().BeTrue();
            product.TryGetProperty("price",        out _).Should().BeTrue();
            product.TryGetProperty("categoryName", out _).Should().BeTrue();
        }
    }

    [Fact]
    public async Task GetProducts_FilterByCategory_ReturnsOnlyCategoryProducts()
    {
        var response = await _client.GetAsync("/api/catalog/products?category=makeup");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var items = doc.RootElement.GetProperty("items").EnumerateArray().ToList();

        items.Should().NotBeEmpty();
        items.All(p => (p.GetProperty("categoryName").GetString() ?? "")
                          .Equals("Makeup", StringComparison.OrdinalIgnoreCase))
             .Should().BeTrue();
    }

    [Fact]
    public async Task GetProducts_SortByPriceAsc_ReturnsSortedAscending()
    {
        var response = await _client.GetAsync("/api/catalog/products?sortBy=price_asc");
        var json     = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var prices = doc.RootElement.GetProperty("items").EnumerateArray()
            .Select(p => p.GetProperty("price").GetDecimal())
            .ToList();

        prices.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetProducts_SortByPriceDesc_ReturnsSortedDescending()
    {
        var response = await _client.GetAsync("/api/catalog/products?sortBy=price_desc");
        var json     = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var prices = doc.RootElement.GetProperty("items").EnumerateArray()
            .Select(p => p.GetProperty("price").GetDecimal())
            .ToList();

        prices.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetProducts_Pagination_ReturnsCorrectPage()
    {
        var response = await _client.GetAsync("/api/catalog/products?page=1&pageSize=2");
        var json     = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("items").GetArrayLength().Should().BeLessOrEqualTo(2);
        doc.RootElement.GetProperty("page").GetInt32().Should().Be(1);
        doc.RootElement.GetProperty("pageSize").GetInt32().Should().Be(2);
    }

    // ── GET /api/catalog/products/{id} ────────────────────────────────────────

    [Fact]
    public async Task GetProductById_ExistingProduct_Returns200()
    {
        // Get the ID of the first seeded product
        var listResp = await _client.GetAsync("/api/catalog/products");
        var listJson = await listResp.Content.ReadAsStringAsync();
        using var listDoc = JsonDocument.Parse(listJson);
        var productId = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetInt32();

        var response = await _client.GetAsync($"/api/catalog/products/{productId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("id").GetInt32().Should().Be(productId);
        doc.RootElement.TryGetProperty("relatedProducts", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetProductById_NonExistent_Returns404WithProblemDetails()
    {
        var response = await _client.GetAsync("/api/catalog/products/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType
            .Should().Be("application/problem+json");
    }

    // ── GET /api/catalog/search ───────────────────────────────────────────────

    [Fact]
    public async Task SearchProducts_ByName_ReturnsMatchingProducts()
    {
        // "Fenty" is in the seeded product name "Pro Filt'r Soft Matte Foundation"
        var response = await _client.GetAsync("/api/catalog/search?q=Fenty");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SearchProducts_NoMatch_ReturnsEmptyItems()
    {
        var response = await _client.GetAsync("/api/catalog/search?q=xyzzy_nonexistent_brand");
        var json     = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(0);
        doc.RootElement.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    // ── GET /api/catalog/categories/{slug}/products ───────────────────────────

    [Fact]
    public async Task GetProductsBySlug_ValidSlug_Returns200()
    {
        var response = await _client.GetAsync("/api/catalog/categories/makeup/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("products", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("category", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetProductsBySlug_InvalidSlug_Returns404()
    {
        var response = await _client.GetAsync("/api/catalog/categories/does-not-exist/products");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
