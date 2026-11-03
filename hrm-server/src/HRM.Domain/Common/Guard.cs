namespace HRM.Domain.Common;

/// <summary>
/// Small checks entities use to protect their invariants. Input format validation (lengths, patterns)
/// belongs to FluentValidation in the Application layer; these only stop clearly invalid state.
/// </summary>
internal static class Guard
{
    public static string Required(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{fieldName} is required.");
        return value.Trim();
    }

    public static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static string Email(string? value)
    {
        var email = Required(value, "Email").ToLowerInvariant();
        if (!email.Contains('@'))
            throw new DomainException("Email is not valid.");
        return email;
    }
}
