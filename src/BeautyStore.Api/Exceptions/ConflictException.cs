namespace BeautyStore.Api.Exceptions;

/// <summary>Maps to HTTP 409. Throw when an operation conflicts with existing state.</summary>
public sealed class ConflictException(string message) : Exception(message);
