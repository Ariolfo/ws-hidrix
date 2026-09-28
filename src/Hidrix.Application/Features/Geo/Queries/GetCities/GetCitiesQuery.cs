using Mediator;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.DTOs;

namespace Hidrix.Application.Features.Geo.Queries.GetCities;

/// <summary>
/// Consulta ciudades de un departamento.
/// </summary>
public class GetCitiesQuery : IRequest<IReadOnlyList<GeoCityDto>>
{
    /// <summary>Identificador del departamento.</summary>
    public int DepoId { get; set; }

    /// <summary>Filtro opcional por nombre/código.</summary>
    public string? Q { get; set; }

    /// <summary>Límite de resultados (default 2000, máx 5000).</summary>
    public int Limit { get; set; } = 2000;
}

/// <summary>
/// Handler de ciudades.
/// </summary>
public class GetCitiesQueryHandler : IRequestHandler<GetCitiesQuery, IReadOnlyList<GeoCityDto>>
{
    private readonly IGeoRepository _geo;

    /// <summary>
    /// Inicializa el handler.
    /// </summary>
    public GetCitiesQueryHandler(IGeoRepository geo)
    {
        _geo = geo;
    }

    /// <summary>
    /// Obtiene ciudades filtradas.
    /// </summary>
    public ValueTask<IReadOnlyList<GeoCityDto>> Handle(
        GetCitiesQuery request,
        CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.Limit <= 0 ? 2000 : request.Limit, 1, 5000);
        return new(_geo.GetCitiesByDepoIdAsync(request.DepoId, request.Q, limit, cancellationToken));
    }
}
