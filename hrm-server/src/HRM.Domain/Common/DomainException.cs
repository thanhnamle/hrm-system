namespace HRM.Domain.Common;

/// <summary>
/// Thrown when a business rule is broken. The API maps it to HTTP 422.
/// </summary>
public class DomainException(string message) : Exception(message);
