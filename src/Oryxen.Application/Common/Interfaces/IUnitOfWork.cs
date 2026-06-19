namespace Oryxen.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the persistence transaction boundary. Implemented by the EF Core
/// DbContext in the Infrastructure layer.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
