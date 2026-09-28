using Microsoft.EntityFrameworkCore;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.DTOs;
using Hidrix.Domain.Entities;
using Hidrix.Infrastructure.Persistence;

namespace Hidrix.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio EF del catálogo geográfico.
/// </summary>
public sealed class GeoRepository : IGeoRepository
{
    private readonly HidrixDbContext _db;

    /// <summary>Inicializa el repositorio.</summary>
    public GeoRepository(HidrixDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GeoCountryDto>> GetCountriesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Paises
            .AsNoTracking()
            .OrderBy(p => p.PaisNombre)
            .Select(p => new GeoCountryDto { Id = p.PaisId, Name = p.PaisNombre })
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GeoDepartmentDto>> GetDepartmentsByPaisIdAsync(
        int paisId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Departamentos
            .AsNoTracking()
            .Where(d => d.PaisId == paisId)
            .OrderBy(d => d.DepoNombre)
            .Select(d => new GeoDepartmentDto
            {
                Id = d.DepoId,
                Code = d.DepoCode,
                Name = d.DepoNombre,
                PaisId = d.PaisId,
            })
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GeoCityDto>> GetCitiesByDepoIdAsync(
        int depoId,
        string? query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var q = _db.Ciudades.AsNoTracking().Where(c => c.DepoId == depoId);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(c =>
                EF.Functions.Like(c.CiuNombre, $"%{term}%") ||
                EF.Functions.Like(c.CiuCod, $"%{term}%"));
        }

        return await q
            .OrderBy(c => c.CiuNombre)
            .Take(limit)
            .Select(c => new GeoCityDto
            {
                Id = c.CiuId,
                Code = c.CiuCod,
                Name = c.CiuNombre,
                DepoId = c.DepoId,
            })
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HidrtbPais>> ListPaisesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Paises.AsNoTracking().ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HidrtbDepartamento>> ListDepartamentosByPaisIdAsync(
        int paisId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Departamentos
            .AsNoTracking()
            .Where(d => d.PaisId == paisId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HidrtbCiudad>> ListCiudadesByDepoIdAsync(
        int depoId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Ciudades
            .AsNoTracking()
            .Where(c => c.DepoId == depoId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserLocationDto?> GetUserLocationByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from u in _db.Users.AsNoTracking()
            join c in _db.Ciudades.AsNoTracking() on u.CiuId equals c.CiuId
            join d in _db.Departamentos.AsNoTracking() on c.DepoId equals d.DepoId
            join p in _db.Paises.AsNoTracking() on d.PaisId equals p.PaisId
            where u.Id == userId
            select new UserLocationDto
            {
                CountryId = p.PaisId,
                DepartmentId = d.DepoId,
                CityId = c.CiuId,
                CountryName = p.PaisNombre,
                DepartmentName = d.DepoNombre,
                CityName = c.CiuNombre,
            }
        ).FirstOrDefaultAsync(cancellationToken);
    }
}
