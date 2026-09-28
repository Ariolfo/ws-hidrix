namespace Hidrix.Application.Common.Interfaces;

/// <summary>
/// Unidad de trabajo: confirma cambios y comprueba conectividad (Repository / UoW).
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persiste los cambios pendientes en la base de datos.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Número de entidades afectadas.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica la conectividad con SQL Server.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>True si la base responde.</returns>
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
}
