namespace BeautyStore.Api.Exceptions;

/// <summary>Maps to HTTP 400. Throw when input fails business validation rules.</summary>
public sealed class ValidationException(
    string message,
    IDictionary<string, string[]>? errors = null) : Exception(message)
{
    /// <summary>Field-level validation errors included in the ProblemDetails response.</summary>
    public IDictionary<string, string[]>? Errors { get; } = errors;
}
