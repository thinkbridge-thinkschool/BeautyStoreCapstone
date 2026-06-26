namespace BeautyStore.IntegrationTests;

/// <summary>
/// End-to-end flow: Register → Login → Create Order → Retrieve Orders → Assert.
/// All steps use real HTTP over the in-memory test server with a SQLite database.
/// </summary>
[Collection("Integration")]
public sealed class E2EFlowTests(BeautyStoreWebApplicationFactory factory)
    : IClassFixture<BeautyStoreWebApplicationFactory>
{
    [Fact]
    public async Task FullPurchaseFlow_RegisterLoginOrderRetrieve()
    {
        var client = factory.CreateClient();

        // ── Step 1: Register ──────────────────────────────────────────────────
        var email    = $"e2e-{Guid.NewGuid():N}@beautystore.test";
        const string fullName = "E2E Shopper";
        const string password = "TestPass1!";

        var registerResp = await client.PostAsJsonAsync("/api/auth/register", new
        {
            FullName = fullName,
            Email    = email,
            Password = password,
        });
        registerResp.StatusCode.Should().Be(HttpStatusCode.OK, "registration must succeed");

        var registerJson = await registerResp.Content.ReadAsStringAsync();
        using var registerDoc = JsonDocument.Parse(registerJson);
        var registerToken = registerDoc.RootElement.GetProperty("accessToken").GetString()!;
        registerToken.Should().NotBeNullOrWhiteSpace();

        // ── Step 2: Login with same credentials ───────────────────────────────
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email    = email,
            Password = password,
        });
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK, "login must succeed");

        var loginJson = await loginResp.Content.ReadAsStringAsync();
        using var loginDoc = JsonDocument.Parse(loginJson);
        var loginToken = loginDoc.RootElement.GetProperty("accessToken").GetString()!;
        loginToken.Should().NotBeNullOrWhiteSpace();

        // ── Step 3: Verify /me returns correct profile ────────────────────────
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginToken);

        var meResp = await client.GetAsync("/api/auth/me");
        meResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var meJson = await meResp.Content.ReadAsStringAsync();
        using var meDoc = JsonDocument.Parse(meJson);
        meDoc.RootElement.GetProperty("email").GetString().Should().Be(email);
        meDoc.RootElement.GetProperty("fullName").GetString().Should().Be(fullName);
        meDoc.RootElement.GetProperty("roles").EnumerateArray()
            .Select(r => r.GetString()).Should().Contain("Customer");

        // ── Step 4: Discover a product to order ───────────────────────────────
        var productsResp = await client.GetAsync("/api/catalog/products");
        productsResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var productsJson = await productsResp.Content.ReadAsStringAsync();
        using var productsDoc = JsonDocument.Parse(productsJson);
        var firstProduct = productsDoc.RootElement.GetProperty("items")[0];
        var productId    = firstProduct.GetProperty("id").GetInt32();
        var productPrice = firstProduct.GetProperty("price").GetDecimal();
        productId.Should().BeGreaterThan(0);

        // ── Step 5: Place an order ────────────────────────────────────────────
        const int quantity = 2;
        var orderResp = await client.PostAsJsonAsync("/api/orders", new
        {
            ProductId = productId,
            Quantity  = quantity,
        });
        orderResp.StatusCode.Should().Be(HttpStatusCode.Created, "order placement must return 201");

        var orderJson = await orderResp.Content.ReadAsStringAsync();
        using var orderDoc = JsonDocument.Parse(orderJson);
        var orderId = orderDoc.RootElement.GetProperty("orderId").GetInt32();
        orderId.Should().BeGreaterThan(0);
        orderDoc.RootElement.GetProperty("status").GetString().Should().Be("Created");

        // ── Step 6: Retrieve my orders ────────────────────────────────────────
        var myOrdersResp = await client.GetAsync("/api/orders/my");
        myOrdersResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var myOrdersJson = await myOrdersResp.Content.ReadAsStringAsync();
        using var myOrdersDoc = JsonDocument.Parse(myOrdersJson);
        myOrdersDoc.RootElement.GetArrayLength().Should().Be(1);

        var placedOrder = myOrdersDoc.RootElement[0];
        placedOrder.GetProperty("orderId").GetInt32().Should().Be(orderId);
        placedOrder.GetProperty("quantity").GetInt32().Should().Be(quantity);
        placedOrder.GetProperty("status").GetString().Should().Be("Created");
        placedOrder.GetProperty("totalPrice").GetDecimal().Should().Be(productPrice * quantity);

        // ── Step 7: Delete the order ──────────────────────────────────────────
        var deleteResp = await client.DeleteAsync($"/api/orders/{orderId}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // ── Step 8: Verify order list is now empty ────────────────────────────
        var afterDeleteResp = await client.GetAsync("/api/orders/my");
        var afterDeleteJson = await afterDeleteResp.Content.ReadAsStringAsync();
        using var afterDeleteDoc = JsonDocument.Parse(afterDeleteJson);
        afterDeleteDoc.RootElement.GetArrayLength().Should().Be(0,
            "the deleted order must no longer appear in the list");
    }

    [Fact]
    public async Task RegisterTwice_SameEmail_SecondRegistrationFails()
    {
        var client = factory.CreateClient();
        var email  = $"e2e-dup-{Guid.NewGuid():N}@beautystore.test";

        // First registration succeeds
        var first = await client.PostAsJsonAsync("/api/auth/register",
            new { FullName = "Original", Email = email, Password = "TestPass1!" });
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second registration with the same email must fail
        var second = await client.PostAsJsonAsync("/api/auth/register",
            new { FullName = "Duplicate", Email = email, Password = "TestPass1!" });
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MultiUser_OrderIsolation()
    {
        var clientA = factory.CreateAuthenticatedClient(
            await factory.RegisterAsync(factory.CreateClient()));
        var clientB = factory.CreateAuthenticatedClient(
            await factory.RegisterAsync(factory.CreateClient()));

        // Discover a product using clientA
        var productsResp = await clientA.GetAsync("/api/catalog/products");
        var productsJson = await productsResp.Content.ReadAsStringAsync();
        using var productsDoc = JsonDocument.Parse(productsJson);
        var productId = productsDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetInt32();

        // A places an order; B should not see it
        await clientA.PostAsJsonAsync("/api/orders", new { ProductId = productId, Quantity = 1 });

        var bOrdersResp = await clientB.GetAsync("/api/orders/my");
        var bOrdersJson = await bOrdersResp.Content.ReadAsStringAsync();
        using var bOrdersDoc = JsonDocument.Parse(bOrdersJson);
        bOrdersDoc.RootElement.GetArrayLength().Should().Be(0,
            "User B must not see User A's orders");

        var aOrdersResp = await clientA.GetAsync("/api/orders/my");
        var aOrdersJson = await aOrdersResp.Content.ReadAsStringAsync();
        using var aOrdersDoc = JsonDocument.Parse(aOrdersJson);
        aOrdersDoc.RootElement.GetArrayLength().Should().Be(1,
            "User A's own order must appear in their list");
    }

    [Fact]
    public async Task Catalog_Search_FindsSeededProduct()
    {
        var client   = factory.CreateClient();
        var response = await client.GetAsync("/api/catalog/search?q=Fenty");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var count = doc.RootElement.GetProperty("items").GetArrayLength();
        count.Should().BeGreaterThan(0, "the seeder includes a Fenty Beauty product");
    }
}
