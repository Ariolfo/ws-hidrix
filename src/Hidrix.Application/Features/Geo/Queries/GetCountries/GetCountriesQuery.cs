using Mediator;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.DTOs;

namespace Hidrix.Application.Features.Geo.Queries.GetCountries;

/// <summary>
/// Consulta el listado de países.
/// </summary>
public class GetCountriesQuery : IRequest<IReadOnlyList<GeoCountryDto>>
{
}

/// <summary>
/// Handler de países.
/// </summary>
public class GetCountriesQueryHandler : IRequestHandler<GetCountriesQuery, IReadOnlyList<GeoCountryDto>>
{
    private readonly IGeoRepository _geo;

    /// <summary>
    /// Inicializa el handler.
    /// </summary>
    public GetCountriesQueryHandler(IGeoRepository geo)
    {
        _geo = geo;
    }

    /// <summary>
    /// Obtiene países ordenados por nombre.
    /// </summary>
    public ValueTask<IReadOnlyList<GeoCountryDto>> Handle(
        GetCountriesQuery request,
        CancellationToken cancellationToken) =>
        new(_geo.GetCountriesAsync(cancellationToken));
}
