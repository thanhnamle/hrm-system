namespace HRM.Application.Common.Exceptions;

/// <summary>The request clashes with existing data, e.g. a duplicate email. Mapped to 409.</summary>
public class ConflictException(string message) : Exception(message);
