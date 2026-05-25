namespace JoyMon.Content;

/// <summary>
/// Captures the outcome of loading and validating content files.
/// </summary>
public class ContentValidationResult
{
    private readonly List<string> _errors = new();

    /// <summary>True when no validation errors were found.</summary>
    public bool IsValid => _errors.Count == 0;

    /// <summary>Read-only view of all validation errors.</summary>
    public IReadOnlyList<string> Errors => _errors.AsReadOnly();

    /// <summary>Add a single validation error.</summary>
    public void AddError(string error) => _errors.Add(error);

    /// <summary>Add multiple validation errors.</summary>
    public void AddErrors(IEnumerable<string> errors) => _errors.AddRange(errors);

    /// <summary>Throw if validation failed, with all errors joined in the message.</summary>
    public void ThrowIfInvalid()
    {
        if (!IsValid)
            throw new InvalidContentException(
                $"Content validation failed with {_errors.Count} error(s):{Environment.NewLine}" +
                string.Join(Environment.NewLine, _errors.Select(e => $"  - {e}")));
    }
}

/// <summary>
/// Exception thrown when content fails validation or loading.
/// </summary>
public class InvalidContentException : Exception
{
    public InvalidContentException(string message) : base(message) { }
    public InvalidContentException(string message, Exception inner) : base(message, inner) { }
}