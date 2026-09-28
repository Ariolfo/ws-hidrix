using Microsoft.EntityFrameworkCore;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.DTOs;
using Hidrix.Domain.Entities;
using Hidrix.Infrastructure.Persistence;

namespace Hidrix.Infrastructure.Persistence.Repositories;

/// <summary>
/// Catálogo de métodos para capacidad de campo.
/// </summary>
public sealed class MetodoCCCatalogService : IMetodoCCCatalogService
{
    private readonly HidrixDbContext _db;

    /// <summary>Inicializa el servicio.</summary>
    public MetodoCCCatalogService(HidrixDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MetodoCCDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.MetodosCC.AsNoTracking()
            .Where(m => m.MetoActivo)
            .OrderBy(m => m.MetoNombre)
            .ToListAsync(cancellationToken);
        return rows.Select(ToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<MetodoCCDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.MetodosCC.AsNoTracking()
            .FirstOrDefaultAsync(m => m.MetoId == id && m.MetoActivo, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    /// <inheritdoc />
    public async Task<MetodoCCDto> CreateAsync(
        CreateMetodoCCRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre del método es obligatorio.");
        }

        var exists = await _db.MetodosCC.AnyAsync(m => m.MetoNombre == name, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Ya existe el método {name}.");
        }

        var now = DateTime.UtcNow;
        var entity = new HidrtbMetodoCC
        {
            MetoNombre = name,
            MetoDescripcion = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
            MetoActivo = true,
            MetoFechaCreacion = now,
            MetoFechaActualizacion = now,
        };
        _db.MetodosCC.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    /// <inheritdoc />
    public async Task<MetodoCCDto> UpdateAsync(
        int id,
        CreateMetodoCCRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.MetodosCC
            .FirstOrDefaultAsync(m => m.MetoId == id && m.MetoActivo, cancellationToken)
            ?? throw new KeyNotFoundException($"No existe el método {id}.");

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre del método es obligatorio.");
        }

        var duplicate = await _db.MetodosCC.AnyAsync(
            m => m.MetoNombre == name && m.MetoId != id,
            cancellationToken);
        if (duplicate)
        {
            throw new InvalidOperationException($"Ya existe el método {name}.");
        }

        entity.MetoNombre = name;
        entity.MetoDescripcion = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();
        entity.MetoFechaActualizacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private static MetodoCCDto ToDto(HidrtbMetodoCC m) => new()
    {
        Id = m.MetoId,
        Name = m.MetoNombre,
        Description = m.MetoDescripcion,
    };
}
