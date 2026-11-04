namespace HRM.Application.Common.Exceptions;

/// <summary>Mapped to 404.</summary>
public class NotFoundException(string entityName, object key) : Exception($"{entityName} '{key}' was not found.");
