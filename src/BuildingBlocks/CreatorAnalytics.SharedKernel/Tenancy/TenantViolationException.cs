namespace CreatorAnalytics.SharedKernel.Tenancy;

/// <summary>
/// Thrown when code tries to write another tenant's data. This is a bug or an attack,
/// never a normal business error, so it must not be turned into a polite HTTP answer.
/// </summary>
public sealed class TenantViolationException : Exception
{
    public TenantViolationException(string message)
        : base(message)
    {
    }
}