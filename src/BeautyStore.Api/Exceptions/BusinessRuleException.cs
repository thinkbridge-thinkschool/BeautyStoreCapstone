namespace BeautyStore.Api.Exceptions;

/// <summary>Maps to HTTP 422. Throw when input is structurally valid but violates a domain rule.</summary>
public sealed class BusinessRuleException(string message) : Exception(message);
