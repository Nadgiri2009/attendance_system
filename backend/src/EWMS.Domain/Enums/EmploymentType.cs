namespace EWMS.Domain.Enums;

/// <summary>
/// Employment type classification for employees.
/// </summary>
public enum EmploymentType
{
    /// <summary>
    /// Permanent employee.
    /// </summary>
    Permanent = 1,

    /// <summary>
    /// Contract-based employment.
    /// </summary>
    Contract = 2,

    /// <summary>
    /// Temporary or seasonal employment.
    /// </summary>
    Temporary = 3,

    /// <summary>
    /// Part-time employment.
    /// </summary>
    PartTime = 4,

    /// <summary>
    /// Intern or apprentice.
    /// </summary>
    Intern = 5
}
