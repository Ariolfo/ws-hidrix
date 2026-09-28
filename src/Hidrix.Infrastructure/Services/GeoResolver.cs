using Hidrix.Application.Common.Exceptions;
using Hidrix.Application.Common.Interfaces;

namespace Hidrix.Infrastructure.Services;

/// <summary>
/// Resolución de nombres geográficos contra el catálogo master.
/// </summary>
public class GeoResolver : IGeoResolver
{
    private const int ColombiaPaisId = 170;
    private readonly IGeoRepository _geo;

    /// <summary>Inicializa el resolvedor.</summary>
    public GeoResolver(IGeoRepository geo)
    {
        _geo = geo;
    }

    /// <inheritdoc />
    public async Task<ResolvedLocation> ResolveAsync(
        string country,
        string department,
        string city,
        CancellationToken cancellationToken = default)
    {
        var countryQ = NormalizePlaceName(country);
        var deptQ = NormalizePlaceName(department);
        var cityQ = NormalizePlaceName(city);

        var paises = await _geo.ListPaisesAsync(cancellationToken);
        var pais = paises
            .Select(p => new
            {
                p.PaisId,
                p.PaisNombre,
                Score =
                    p.PaisId.ToString() == countryQ ? 0 :
                    string.Equals(p.PaisNombre, countryQ, StringComparison.OrdinalIgnoreCase) ? 1 :
                    p.PaisNombre.Contains(countryQ, StringComparison.OrdinalIgnoreCase) ? 2 :
                    countryQ.StartsWith("colombia", StringComparison.OrdinalIgnoreCase) && p.PaisId == ColombiaPaisId ? 3 :
                    99,
            })
            .Where(x => x.Score < 99)
            .OrderBy(x => x.Score)
            .FirstOrDefault()
            ?? throw new AppException($"País no reconocido: {country}");

        var dept = await _geo.ListDepartamentosByPaisIdAsync(pais.PaisId, cancellationToken);

        var matchedDept = dept
            .Select(d => new
            {
                d.DepoId,
                d.DepoCode,
                d.DepoNombre,
                Score =
                    string.Equals(d.DepoCode, deptQ, StringComparison.OrdinalIgnoreCase) ? 0 :
                    string.Equals(d.DepoNombre, deptQ, StringComparison.OrdinalIgnoreCase) ? 0 :
                    d.DepoNombre.Contains(deptQ, StringComparison.OrdinalIgnoreCase) ? 1 :
                    99,
            })
            .Where(x => x.Score < 99)
            .OrderBy(x => x.Score)
            .ThenBy(x => x.DepoNombre)
            .FirstOrDefault()
            ?? throw new AppException($"Departamento no reconocido: {department}");

        var ciudades = await _geo.ListCiudadesByDepoIdAsync(matchedDept.DepoId, cancellationToken);

        var matchedCity = ciudades
            .Select(c => new
            {
                c.CiuId,
                c.CiuCod,
                c.CiuNombre,
                Score =
                    string.Equals(c.CiuCod, cityQ, StringComparison.OrdinalIgnoreCase) ? 0 :
                    string.Equals(c.CiuNombre, cityQ, StringComparison.OrdinalIgnoreCase) ? 0 :
                    c.CiuNombre.Contains(cityQ, StringComparison.OrdinalIgnoreCase) ? 1 :
                    99,
            })
            .Where(x => x.Score < 99)
            .OrderBy(x => x.Score)
            .ThenBy(x => x.CiuNombre)
            .FirstOrDefault()
            ?? throw new AppException($"Ciudad no reconocida: {city}");

        return new ResolvedLocation
        {
            CiuId = matchedCity.CiuId,
            PaisId = pais.PaisId,
            DepoId = matchedDept.DepoId,
            CodigoPais = pais.PaisId.ToString(),
            CodigoDepartamento = matchedDept.DepoCode,
            CodigoCiudad = matchedCity.CiuCod,
            PaisNombre = pais.PaisNombre,
            DepartamentoNombre = matchedDept.DepoNombre,
            CiudadNombre = matchedCity.CiuNombre,
        };
    }

    private static string NormalizePlaceName(string value)
    {
        var name = value.Trim();
        foreach (var suffix in new[]
                 {
                     " Department", " department", " Province", " province", " State", " state",
                 })
        {
            if (name.EndsWith(suffix, StringComparison.Ordinal))
            {
                name = name[..^suffix.Length].Trim();
            }
        }

        return name;
    }
}
