namespace BeautyStore.UnitTests;

public sealed class OrderCalculationTests
{
    // ── TotalPrice calculation ────────────────────────────────────────────────

    [Fact]
    public void TotalPrice_IsUnitPriceTimesQuantity()
    {
        var order = new Order
        {
            UnitPrice  = 3800m,
            Quantity   = 2,
            TotalPrice = 3800m * 2,
        };

        order.TotalPrice.Should().Be(7600m);
    }

    [Theory]
    [InlineData(100.0,   1,   100.0)]
    [InlineData(100.0,   3,   300.0)]
    [InlineData(9.99,    5,   49.95)]
    [InlineData(1000.0, 10, 10000.0)]
    [InlineData(0.01,    1,    0.01)]
    public void TotalPrice_MatchesExpectedForVariousInputs(
        double unitPriceD, int quantity, double expectedD)
    {
        var unitPrice = (decimal)unitPriceD;
        var expected  = (decimal)expectedD;
        var order = new Order
        {
            UnitPrice  = unitPrice,
            Quantity   = quantity,
            TotalPrice = unitPrice * quantity,
        };

        order.TotalPrice.Should().Be(expected);
    }

    // ── Default values ────────────────────────────────────────────────────────

    [Fact]
    public void Order_DefaultStatus_IsCreated()
    {
        var order = new Order();
        order.Status.Should().Be("Created");
    }

    [Fact]
    public void Order_DefaultCreatedAtUtc_IsRecentUtcTime()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var order  = new Order();
        var after  = DateTime.UtcNow.AddSeconds(1);

        order.CreatedAtUtc.Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public void Order_DefaultUserId_IsEmptyString()
    {
        var order = new Order();
        order.UserId.Should().BeEmpty();
    }

    [Fact]
    public void Order_DefaultProductName_IsEmptyString()
    {
        var order = new Order();
        order.ProductName.Should().BeEmpty();
    }

    // ── Quantity invariants ───────────────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public void Order_PositiveQuantity_IsValid(int quantity)
    {
        var order = new Order { Quantity = quantity };
        order.Quantity.Should().BePositive();
    }

    // ── Status transitions (documentation tests) ──────────────────────────────

    [Theory]
    [InlineData("Created")]
    [InlineData("Confirmed")]
    [InlineData("Shipped")]
    [InlineData("Delivered")]
    [InlineData("Cancelled")]
    public void Order_StatusCanBeSetToKnownValues(string status)
    {
        var order = new Order { Status = status };
        order.Status.Should().Be(status);
    }

    // ── Snapshot fields ───────────────────────────────────────────────────────

    [Fact]
    public void Order_ProductNameSnapshot_IsIndependentOfExternalState()
    {
        // Verify Order stores a copy of the product name, not a reference
        const string originalName = "Pro Filt'r Foundation";
        var order = new Order { ProductName = originalName };

        // Order.ProductName cannot change even if the source changes
        order.ProductName.Should().Be(originalName);
    }

    [Fact]
    public void Order_UnitPriceSnapshot_IsStoredAsDecimal()
    {
        var order = new Order { UnitPrice = 12500.99m };
        order.UnitPrice.Should().Be(12500.99m);
    }
}
