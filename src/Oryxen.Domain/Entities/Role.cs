namespace Oryxen.Domain.Entities;

/// <summary>
/// RBAC role granted to a <see cref="UserAccount"/>. The <see cref="Name"/> stores the
/// canonical uppercase identifier (e.g. FARMER, ADMIN, SUPPORT_TECHNICIAN) that is
/// emitted as a role claim inside the JWT.
/// </summary>
public class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public ICollection<UserAccount> Users { get; set; } = new List<UserAccount>();
}
