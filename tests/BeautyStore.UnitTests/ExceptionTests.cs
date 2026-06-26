namespace BeautyStore.UnitTests;

public sealed class ExceptionTests
{
    // ── ValidationException ───────────────────────────────────────────────────

    [Fact]
    public void ValidationException_StoresMessage()
    {
        var ex = new ValidationException("Name is required.");
        ex.Message.Should().Be("Name is required.");
    }

    [Fact]
    public void ValidationException_StoresFieldErrors()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["email"]    = ["Email is invalid."],
            ["password"] = ["Too short.", "Needs a digit."],
        };

        var ex = new ValidationException("Validation failed.", errors);

        ex.Errors.Should().NotBeNull();
        ex.Errors!["email"].Should().ContainSingle().Which.Should().Be("Email is invalid.");
        ex.Errors["password"].Should().HaveCount(2);
    }

    [Fact]
    public void ValidationException_NullErrors_IsAllowed()
    {
        var ex = new ValidationException("Bad input.");
        ex.Errors.Should().BeNull();
    }

    [Fact]
    public void ValidationException_InheritsFromException()
    {
        var ex = new ValidationException("x");
        ex.Should().BeAssignableTo<Exception>();
    }

    // ── NotFoundException ─────────────────────────────────────────────────────

    [Fact]
    public void NotFoundException_StoresMessage()
    {
        var ex = new NotFoundException("Product 42 not found.");
        ex.Message.Should().Be("Product 42 not found.");
    }

    [Fact]
    public void NotFoundException_InheritsFromException()
    {
        var ex = new NotFoundException("x");
        ex.Should().BeAssignableTo<Exception>();
    }

    // ── ConflictException ─────────────────────────────────────────────────────

    [Fact]
    public void ConflictException_StoresMessage()
    {
        var ex = new ConflictException("Email already registered.");
        ex.Message.Should().Be("Email already registered.");
    }

    [Fact]
    public void ConflictException_InheritsFromException()
    {
        var ex = new ConflictException("x");
        ex.Should().BeAssignableTo<Exception>();
    }

    // ── ForbiddenException ────────────────────────────────────────────────────

    [Fact]
    public void ForbiddenException_StoresMessage()
    {
        var ex = new ForbiddenException("Not your resource.");
        ex.Message.Should().Be("Not your resource.");
    }

    [Fact]
    public void ForbiddenException_InheritsFromException()
    {
        var ex = new ForbiddenException("x");
        ex.Should().BeAssignableTo<Exception>();
    }

    // ── BusinessRuleException ─────────────────────────────────────────────────

    [Fact]
    public void BusinessRuleException_StoresMessage()
    {
        var ex = new BusinessRuleException("Cannot cancel a shipped order.");
        ex.Message.Should().Be("Cannot cancel a shipped order.");
    }

    [Fact]
    public void BusinessRuleException_InheritsFromException()
    {
        var ex = new BusinessRuleException("x");
        ex.Should().BeAssignableTo<Exception>();
    }

    // ── Distinct types ────────────────────────────────────────────────────────

    [Fact]
    public void AllExceptions_AreDistinctTypes()
    {
        var validation = new ValidationException("x");
        var notFound   = new NotFoundException("x");
        var conflict   = new ConflictException("x");
        var forbidden  = new ForbiddenException("x");
        var business   = new BusinessRuleException("x");

        validation.GetType().Should().NotBe(notFound.GetType());
        validation.GetType().Should().NotBe(conflict.GetType());
        validation.GetType().Should().NotBe(forbidden.GetType());
        validation.GetType().Should().NotBe(business.GetType());
        notFound.GetType().Should().NotBe(conflict.GetType());
    }
}
