using Hidrix.Application.DTOs;
using Hidrix.Application.Services;

namespace Hidrix.Application.Common.Interfaces;

/// <summary>
/// Metadatos Hidrix de sensores Visualiti (cultivo, finca, CC).
/// El inventario de estaciones vive en Visualiti.
/// </summary>
public interface ISensorCatalogService
{
    /// <summary>Lista metadatos activos como PhysicalSensor (sin coords Visualiti).</summary>
    Task<IReadOnlyList<PhysicalSensor>> ListSensorsAsync(CancellationToken cancellationToken = default);

    /// <summary>Busca metadatos por serial físico o id lógico.</summary>
    Task<PhysicalSensor?> GetSensorAsync(string sensorId, CancellationToken cancellationToken = default);

    /// <summary>Lista Visualiti + metadatos (cultivo/CC) para administración.</summary>
    Task<IReadOnlyList<CatalogSensorDto>> ListCatalogAsync(CancellationToken cancellationToken = default);

    /// <summary>Obtiene metadatos por id interno.</summary>
    Task<CatalogSensorDto?> GetCatalogByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Crea o actualiza la relación sensor↔cultivo.</summary>
    Task<CatalogSensorDto> AssignCropAsync(
        AssignSensorCropRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Actualiza cultivo/finca de un metadato existente.</summary>
    Task<CatalogSensorDto> UpdateCropAsync(
        int id,
        AssignSensorCropRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Quita la relación (borra cultivo/finca o inactiva el metadato).</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Guarda CC estimada, método y fecha en HidrtbSensorMeta.</summary>
    Task<CatalogSensorDto> SaveEstimatedFieldCapacityAsync(
        int id,
        SaveEstimatedFieldCapacityRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Lista redes con país.</summary>
    Task<IReadOnlyList<NetworkDto>> ListNetworksAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Catálogo de cultivos persistido.
/// </summary>
public interface ICropCatalogService
{
    /// <summary>Lista cultivos activos.</summary>
    Task<IReadOnlyList<CropDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Obtiene un cultivo por id.</summary>
    Task<CropDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Crea un cultivo.</summary>
    Task<CropDto> CreateAsync(CreateCropRequest request, CancellationToken cancellationToken = default);

    /// <summary>Actualiza un cultivo.</summary>
    Task<CropDto> UpdateAsync(int id, CreateCropRequest request, CancellationToken cancellationToken = default);

    /// <summary>Inactiva un cultivo (borrado lógico).</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Resuelve perfil por nombre/texto (fallback Cacao).</summary>
    Task<CropMoistureProfile> ResolveAsync(string? cultivoOrText, CancellationToken cancellationToken = default);
}

/// <summary>
/// Catálogo de métodos para determinar capacidad de campo.
/// </summary>
public interface IMetodoCCCatalogService
{
    /// <summary>Lista métodos activos.</summary>
    Task<IReadOnlyList<MetodoCCDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Obtiene por id.</summary>
    Task<MetodoCCDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Crea un método.</summary>
    Task<MetodoCCDto> CreateAsync(CreateMetodoCCRequest request, CancellationToken cancellationToken = default);

    /// <summary>Actualiza un método.</summary>
    Task<MetodoCCDto> UpdateAsync(int id, CreateMetodoCCRequest request, CancellationToken cancellationToken = default);
}
