using FluentAssertions;
using Hidrix.Application.Services;

namespace Hidrix.Application.Tests;

/// <summary>
/// Pruebas de utilidades de sensores (ids, distancia, mapeo lógico).
/// </summary>
public class SensorCatalogTests
{
    [Fact]
    public void HaversineM_SamePoint_ReturnsZero()
    {
        var d = SensorCatalog.HaversineM(4.52, -76.07, 4.52, -76.07);
        d.Should().BeApproximately(0, 0.01);
    }

    [Fact]
    public void HaversineM_KnownDistance_IsReasonable()
    {
        // M316 (4.52198, -76.07732) vs M312 (4.53281, -76.0704) ~1.4 km
        var d = SensorCatalog.HaversineM(4.52198, -76.07732, 4.53281, -76.0704);
        d.Should().BeGreaterThan(1000);
        d.Should().BeLessThan(2000);
    }

    [Fact]
    public void SplitLogicalId_WithChannel_ReturnsPhysicalAndChannel()
    {
        var (physical, channel) = SensorCatalog.SplitLogicalId("M316-1");
        physical.Should().Be("M316");
        channel.Should().Be(1);
    }

    [Fact]
    public void SplitLogicalId_WithoutChannel_ReturnsNullChannel()
    {
        var (physical, channel) = SensorCatalog.SplitLogicalId("M316");
        physical.Should().Be("M316");
        channel.Should().BeNull();
    }

    [Fact]
    public void EncodeStationId_Finca_UsesFinPrefix()
    {
        StationIds.EncodeStationId("Finca San Antonio").Should().Be("fin-finca-san-antonio");
    }

    [Fact]
    public void ToLogicalSensorDtos_ExpandsChannels()
    {
        var sensor = new PhysicalSensor(
            "M316", "RED ASORUT", "Aguacate", "Finca San Antonio", "COLOMBIA", 4.52198, -76.07732);
        var dtos = SensorMapper.ToLogicalSensorDtos(sensor, "fin-finca-san-antonio", null);

        dtos.Should().HaveCount(2);
        dtos[0].Id.Should().Be("M316-1");
        dtos[1].Id.Should().Be("M316-2");
    }
}
