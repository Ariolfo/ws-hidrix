using Hidrix.Application.Common.Interfaces;
using Hidrix.Infrastructure.Persistence;

namespace Hidrix.Infrastructure.Persistence;

/// <summary>
/// Unidad de trabajo sobre <see cref="HidrixDbContext"/>.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly HidrixDbContext _db;

    /// <summary>
    /// Inicializa la unidad de trabajo.
    /// </summary>
    public UnitOfWork(HidrixDbContext db)
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
