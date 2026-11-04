namespace HRM.Application.Common.Exceptions;

/// <summary>The caller is authenticated but may not touch this record (data scope). Mapped to 403.</summary>
public class ForbiddenAccessException(string message = "You do not have access to this resource.") : Exception(message);
