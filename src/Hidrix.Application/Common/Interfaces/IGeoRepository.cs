using Hidrix.Application.DTOs;
using Hidrix.Domain.Entities;

namespace Hidrix.Application.Common.Interfaces;

/// <summary>
/// Repositorio del catálogo geográfico (país / departamento / ciudad).
/// </summary>
public interface IGeoRepository
{
    /// <summary>Lista países para la API (DTO).</summary>
    Task<IReadOnlyList<GeoCountryDto>> GetCountriesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Lista departamentos de un país (DTO).</summary>
    Task<IReadOnlyList<GeoDepartmentDto>> GetDepartmentsByPaisIdAsync(
        int paisId,
        CancellationToken cancellationToken = default);

    /// <summary>Lista ciudades de un departamento con filtro opcional (DTO).</summary>
    Task<IReadOnlyList<GeoCityDto>> GetCitiesByDepoIdAsync(
        int depoId,
        string? query,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>Todos los países (entidades).</summary>
    Task<IReadOnlyList<HidrtbPais>> ListPaisesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Departamentos de un país (entidades).</summary>
    Task<IReadOnlyList<HidrtbDepartamento>> ListDepartamentosByPaisIdAsync(
        int paisId,
        CancellationToken cancellationToken = default);

    /// <summary>Ciudades de un departamento (entidades).</summary>
    Task<IReadOnlyList<HidrtbCiudad>> ListCiudadesByDepoIdAsync(
        int depoId,
        CancellationToken cancellationToken = default);

    /// <summary>Ubicación del usuario según su ciudad registrada.</summary>
    Task<UserLocationDto?> GetUserLocationByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
