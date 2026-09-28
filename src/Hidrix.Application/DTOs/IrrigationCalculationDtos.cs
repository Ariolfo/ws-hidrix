namespace Hidrix.Application.DTOs;

/// <summary>DTO de registro de calculadora de riego.</summary>
public sealed class IrrigationCalculationDto
{
    public int Id { get; set; }
    public string CropName { get; set; } = string.Empty;
    public int? CropId { get; set; }
    public double FieldCapacity { get; set; }
    public double MaxIrrigationLimit { get; set; }
    public double IrrigationDecision { get; set; }
    public string ConsultationDate { get; set; } = string.Empty;
    public double MorningMoisture { get; set; }
    public double AfternoonMoisture { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public string? IrrigationAction { get; set; }
    public string? Observation { get; set; }
}

/// <summary>Request para crear un registro de calculadora.</summary>
public sealed class CreateIrrigationCalculationRequest
{
    public string CropName { get; set; } = string.Empty;
    public int? CropId { get; set; }
    public double FieldCapacity { get; set; }
    public double MaxIrrigationLimit { get; set; }
    public double IrrigationDecision { get; set; }
    public string ConsultationDate { get; set; } = string.Empty;
    public double MorningMoisture { get; set; }
    public double AfternoonMoisture { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public string? IrrigationAction { get; set; }
    public string? Observation { get; set; }
}
