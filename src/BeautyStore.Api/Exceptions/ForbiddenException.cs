namespace BeautyStore.Api.Exceptions;

/// <summary>Maps to HTTP 403. Throw when the caller is authenticated but not permitted.</summary>
public sealed class ForbiddenException(string message) : Exception(message);
