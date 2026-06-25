namespace BeautyStore.Api.Exceptions;

/// <summary>Maps to HTTP 404. Throw when a requested resource does not exist.</summary>
public sealed class NotFoundException(string message) : Exception(message);
