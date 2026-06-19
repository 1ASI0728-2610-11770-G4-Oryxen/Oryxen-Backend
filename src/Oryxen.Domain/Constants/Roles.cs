namespace Oryxen.Domain.Constants;

/// <summary>
/// Canonical role names used for Role-Based Access Control (RBAC) across the solution.
/// These string values are issued as role claims inside the JWT and consumed by
/// <c>[Authorize(Roles = ...)]</c> policies.
/// </summary>
public static class Roles
{
    public const string Farmer = "FARMER";
    public const string Admin = "ADMIN";
    public const string SupportTechnician = "SUPPORT_TECHNICIAN";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Farmer,
        Admin,
        SupportTechnician
    };
}
