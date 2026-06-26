namespace BeautyStore.IntegrationTests;

[Collection("Integration")]
public sealed class OrderEndpointsTests(BeautyStoreWebApplicationFactory factory)
    : IClassFixture<BeautyStoreWebApplicationFactory>
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(HttpClient Client, string Token)> AuthClientAsync()
    {
        var client = factory.CreateClient();
        var token  = await factory.RegisterAsync(client);
        return (client, token);
    }

    private static async Task<int> GetFirstProductIdAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/catalog/products");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("items")[0].GetProperty("id").GetInt32();
    }

    // ── POST /api/orders ──────────────────────────────────────────────────────

    [Fact]
    public async Task PlaceOrder_WithoutAuth_Returns401()
    {
        var client   = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/orders",
            new { ProductId = 1, Quantity = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PlaceOrder_ValidRequest_Returns201WithOrderId()
    {
        var (client, token) = await AuthClientAsync();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var productId = await GetFirstProductIdAsync(client);

        var response = await client.PostAsJsonAsync("/api/orders",
            new { ProductId = productId, Quantity = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("orderId").GetInt32().Should().BeGreaterThan(0);
        doc.RootElement.GetProperty("status").GetString().Should().Be("Created");
    }

    [Fact]
    public async Task PlaceOrder_Quantity0_Returns400WithProblemDetails()
    {
        var (client, token) = await AuthClientAsync();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var productId = await GetFirstProductIdAsync(client);

        var response = await client.PostAsJsonAsync("/api/orders",
            new { ProductId = productId, Quantity = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType
            .Should().Be("application/problem+json");
    }

    [Fact]
    public async Task PlaceOrder_NegativeQuantity_Returns400()
    {
        var (client, token) = await AuthClientAsync();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/orders",
            new { ProductId = 1, Quantity = -5 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PlaceOrder_NonExistentProduct_Returns404()
    {
        var (client, token) = await AuthClientAsync();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/orders",
            new { ProductId = 999999, Quantity = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PlaceOrder_TotalPrice_IsUnitPriceTimesQuantity()
    {
        var (client, token) = await AuthClientAsync();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var productId = await GetFirstProductIdAsync(client);

        // Get the product price first
        var productResponse = await client.GetAsync($"/api/catalog/products/{productId}");
        var productJson     = await productResponse.Content.ReadAsStringAsync();
        using var productDoc = JsonDocument.Parse(productJson);
        var price = productDoc.RootElement.GetProperty("price").GetDecimal();

        // Place order with quantity 3
        await client.PostAsJsonAsync("/api/orders",
            new { ProductId = productId, Quantity = 3 });

        // Verify total price via GET /api/orders/my
        var myOrdersResp = await client.GetAsync("/api/orders/my");
        var ordersJson   = await myOrdersResp.Content.ReadAsStringAsync();
        using var ordersDoc = JsonDocument.Parse(ordersJson);
        var order = ordersDoc.RootElement.EnumerateArray().First();

        order.GetProperty("totalPrice").GetDecimal()
            .Should().Be(price * 3);
    }

    // ── GET /api/orders/my ────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyOrders_WithoutAuth_Returns401()
    {
        var client   = factory.CreateClient();
        var response = await client.GetAsync("/api/orders/my");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyOrders_NoOrders_ReturnsEmptyArray()
    {
        var (client, token) = await AuthClientAsync();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/orders/my");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetMyOrders_AfterPlacingOrder_ContainsTheOrder()
    {
        var (client, token) = await AuthClientAsync();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var productId = await GetFirstProductIdAsync(client);
        await client.PostAsJsonAsync("/api/orders",
            new { ProductId = productId, Quantity = 2 });

        var response = await client.GetAsync("/api/orders/my");
        var json     = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.GetArrayLength().Should().Be(1);
        var order = doc.RootElement[0];
        order.GetProperty("quantity").GetInt32().Should().Be(2);
        order.GetProperty("status").GetString().Should().Be("Created");
    }

    [Fact]
    public async Task GetMyOrders_OnlyReturnsOwnOrders_NotOtherUsers()
    {
        // User A places an order
        var (clientA, tokenA) = await AuthClientAsync();
        clientA.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
        var productId = await GetFirstProductIdAsync(clientA);
        await clientA.PostAsJsonAsync("/api/orders",
            new { ProductId = productId, Quantity = 1 });

        // User B should see zero orders
        var (clientB, tokenB) = await AuthClientAsync();
        clientB.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenB);

        var response = await clientB.GetAsync("/api/orders/my");
        var json     = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.GetArrayLength().Should().Be(0,
            "User B should not see User A's orders");
    }

    // ── DELETE /api/orders/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteOrder_WithoutAuth_Returns401()
    {
        var client   = factory.CreateClient();
        var response = await client.DeleteAsync("/api/orders/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteOrder_OwnOrder_Returns204()
    {
        var (client, token) = await AuthClientAsync();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var productId = await GetFirstProductIdAsync(client);
        var createResp = await client.PostAsJsonAsync("/api/orders",
            new { ProductId = productId, Quantity = 1 });

        var createJson = await createResp.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var orderId = createDoc.RootElement.GetProperty("orderId").GetInt32();

        var deleteResp = await client.DeleteAsync($"/api/orders/{orderId}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteOrder_NonExistentOrder_Returns404()
    {
        var (client, token) = await AuthClientAsync();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.DeleteAsync("/api/orders/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteOrder_AnotherUsersOrder_Returns403()
    {
        // User A places an order
        var (clientA, tokenA) = await AuthClientAsync();
        clientA.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
        var productId = await GetFirstProductIdAsync(clientA);
        var createResp = await clientA.PostAsJsonAsync("/api/orders",
            new { ProductId = productId, Quantity = 1 });
        var createJson = await createResp.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var orderId = createDoc.RootElement.GetProperty("orderId").GetInt32();

        // User B tries to delete it
        var (clientB, tokenB) = await AuthClientAsync();
        clientB.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenB);

        var deleteResp = await clientB.DeleteAsync($"/api/orders/{orderId}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
