namespace Hidrix.Application.DTOs;

/// <summary>DTO de nota de evento de riego.</summary>
public sealed class IrrigationEventNoteDto
{
    public int Id { get; set; }
    public string PlotName { get; set; } = string.Empty;
    public string CropName { get; set; } = string.Empty;
    public int? CropId { get; set; }
    public string EventDate { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string IrrigationType { get; set; } = string.Empty;
    public string IrrigationTypeLabel { get; set; } = string.Empty;
    public double? FlowRateLph { get; set; }
    public string? SoilType { get; set; }
    public int? CityId { get; set; }
    public string? DepartmentName { get; set; }
    public string? CityName { get; set; }
}

/// <summary>Request para crear una nota de evento de riego.</summary>
public sealed class CreateIrrigationEventNoteRequest
{
    public string PlotName { get; set; } = string.Empty;
    public string CropName { get; set; } = string.Empty;
    public int? CropId { get; set; }
    public string EventDate { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string IrrigationType { get; set; } = string.Empty;
    public double? FlowRateLph { get; set; }
    public string? SoilType { get; set; }
    public int CityId { get; set; }
}
