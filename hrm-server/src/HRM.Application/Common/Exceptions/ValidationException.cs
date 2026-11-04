using FluentValidation.Results;

namespace HRM.Application.Common.Exceptions;

/// <summary>Request input is invalid. Mapped to 400 with the field errors.</summary>
public class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this(failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.Distinct().ToArray()))
    {
    }
}
