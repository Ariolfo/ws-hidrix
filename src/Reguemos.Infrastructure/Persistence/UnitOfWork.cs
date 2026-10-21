using Reguemos.Application.Common.Interfaces;
using Reguemos.Infrastructure.Persistence;

namespace Reguemos.Infrastructure.Persistence;

/// <summary>
/// Unidad de trabajo sobre <see cref="ReguemosDbContext"/>.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ReguemosDbContext _db;

    /// <summary>
    /// Inicializa la unidad de trabajo.
    /// </summary>
    public UnitOfWork(ReguemosDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default) =>
        _db.Database.CanConnectAsync(cancellationToken);
}
