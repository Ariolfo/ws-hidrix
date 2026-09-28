using Mediator;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.DTOs;

namespace Hidrix.Application.Features.Geo.Queries.GetDepartments;

/// <summary>
/// Consulta departamentos de un país.
/// </summary>
public class GetDepartmentsQuery : IRequest<IReadOnlyList<GeoDepartmentDto>>
{
    /// <summary>Identificador del país.</summary>
    public int PaisId { get; set; }
}

/// <summary>
/// Handler de departamentos.
/// </summary>
public class GetDepartmentsQueryHandler : IRequestHandler<GetDepartmentsQuery, IReadOnlyList<GeoDepartmentDto>>
{
    private readonly IGeoRepository _geo;

    /// <summary>
    /// Inicializa el handler.
    /// </summary>
    public GetDepartmentsQueryHandler(IGeoRepository geo)
    {
        _geo = geo;
    }

    /// <summary>
    /// Obtiene departamentos del país.
    /// </summary>
    public ValueTask<IReadOnlyList<GeoDepartmentDto>> Handle(
        GetDepartmentsQuery request,
        CancellationToken cancellationToken) =>
        new(_geo.GetDepartmentsByPaisIdAsync(request.PaisId, cancellationToken));
}
