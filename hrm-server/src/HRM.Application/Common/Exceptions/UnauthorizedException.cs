namespace HRM.Application.Common.Exceptions;

/// <summary>The caller is not (or no longer) authenticated. Mapped to 401.</summary>
public class UnauthorizedException(string message) : Exception(message);
