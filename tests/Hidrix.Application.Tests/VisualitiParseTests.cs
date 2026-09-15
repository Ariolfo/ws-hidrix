using System.Text.Json;
using FluentAssertions;
using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.Services;
using Hidrix.Infrastructure.Options;
using Hidrix.Infrastructure.Services;

namespace Hidrix.Application.Tests;

/// <summary>
/// Pruebas del parseo de respuestas Visualiti.
/// </summary>
public class VisualitiParseTests
{
    [Theory]
    [InlineData("2026-07-15 9:00", "America/Bogota", 9, 0, -5)]
    [InlineData("2026-07-15 09:00", "America/Bogota", 9, 0, -5)]
    [InlineData("2026-07-15 9:00:00", "America/Bogota", 9, 0, -5)]
    [InlineData("2026-07-15 9:00", "America/Tegucigalpa", 9, 0, -6)]
    public void ParseReadingTimestamp_InterpretsVisualitiAsCountryLocalTime(
        string raw,
        string timeZoneId,
        int localHour,
        int localMinute,
        int expectedOffsetHours)
    {
        var ts = VisualitiClient.ParseReadingTimestamp(raw, timeZoneId);
        ts.Hour.Should().Be(localHour);
        ts.Minute.Should().Be(localMinute);
        ts.Offset.Should().Be(TimeSpan.FromHours(expectedOffsetHours));
        ts.UtcDateTime.Hour.Should().Be(localHour - expectedOffsetHours);
    }

    [Fact]
    public void ParsePayload_NotDataFound_ReturnsEmpty()
    {
        using var doc = JsonDocument.Parse("""{"device":"SUELO M316","detail":"Not Data Found"}""");
        VisualitiClient.ParsePayload(doc.RootElement).Should().BeEmpty();
    }

    [Fact]
    public void ParsePayload_VolumetricoChannels_ConvertsToPercent()
    {
        const string json = """
        {
          "device": "AGUACATE2 SUELO M316",
          "data": {
            "2026-07-15 9:00": [
              { "time": "2026-07-15 9:00", "sensor": "Volumetrico1", "value": 0.55, "medida": null },
              { "time": "2026-07-15 9:00", "sensor": "Volumetrico2", "value": 0.36, "medida": null }
            ]
          }
        }
        """;
        using var doc = JsonDocument.Parse(json);
        var rows = VisualitiClient.ParsePayload(doc.RootElement, 316, "America/Bogota");
        rows.Should().HaveCount(1);
        rows[0].FechaHora.Hour.Should().Be(9);
        rows[0].FechaHora.Offset.Should().Be(TimeSpan.FromHours(-5));
        rows[0].Valores[1].Should().BeApproximately(55.0, 0.01);
        rows[0].Valores[2].Should().BeApproximately(36.0, 0.01);
    }

    [Fact]
    public void ParseVisualitiStationId_AcceptsAnyValidSerial()
    {
        VisualitiClient.ParseVisualitiStationId("M316-1").Should().Be(316);
        VisualitiClient.ParseVisualitiStationId("M316").Should().Be(316);
        VisualitiClient.ParseVisualitiStationId("M999").Should().Be(999);
        VisualitiClient.ParseVisualitiStationId("invalid").Should().BeNull();
    }

    [Fact]
    public void ParseStationSensors_DeduplicatesChannels()
    {
        const string json = """
        [
          ["Cont Vol1", "Cont Vol1"],
          ["Cont Vol2", "Cont Vol2"],
          ["Volumetrico1", "Volumetrico1"],
          ["Volumetrico2", "Volumetrico2"]
        ]
        """;
        using var doc = JsonDocument.Parse(json);
        var parsed = VisualitiClient.ParseStationSensors(doc.RootElement);
        parsed.Channels.Should().Equal(1, 2);
    }

    [Fact]
    public void VisualitiHardwareDefaults_NullSnapshot_IsOfflineUnknown()
    {
        var snapshot = VisualitiHardwareDefaults.From(null);
        snapshot.HasSnapshot.Should().BeFalse();
        snapshot.Online.Should().BeFalse();
        snapshot.Connectivity.Should().Be("offline");
        snapshot.HardwareStatus.Should().Be("desconocido");
        snapshot.Latitude.Should().BeNull();
        snapshot.Longitude.Should().BeNull();
    }

    [Fact]
    public void VisualitiHardwareDefaults_OnlineSnapshot_PreservesCoords()
    {
        var hw = new VisualitiHardwareStatus
        {
            StationId = 316,
            Latitude = 4.52,
            Longitude = -76.07,
            Online = true,
            Connectivity = "online",
            SensorState = "bueno",
        };
        var snapshot = VisualitiHardwareDefaults.From(hw);
        snapshot.HasSnapshot.Should().BeTrue();
        snapshot.Online.Should().BeTrue();
        snapshot.Connectivity.Should().Be("online");
        snapshot.HardwareStatus.Should().Be("bueno");
        snapshot.Latitude.Should().BeApproximately(4.52, 0.001);
    }

    [Fact]
    public void Enricher_ApplyVisualiti_UsesHardwareDefaultsOn404()
    {
        var baseSensor = new PhysicalSensor("M316", "Visualiti", cultivo: "Aguacate");
        var enriched = VisualitiStationEnricher.ApplyVisualiti(
            baseSensor,
            new VisualitiStationSensors { Channels = [1, 2] },
            hardware: null);

        enriched.Canales.Should().Be(2);
        enriched.Online.Should().BeFalse();
        enriched.Connectivity.Should().Be("offline");
        enriched.HardwareStatus.Should().Be("desconocido");
        enriched.HasHardwareSnapshot.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "", "https://api.appgricultor.com", "https://api.appgricultor.com/api/login")]
    [InlineData("https://api.appgricultor.com", "", "https://api.appgricultor.com", "https://api.appgricultor.com/api/login")]
    [InlineData("", "http://appgricultor.com/api/login", "https://api.appgricultor.com", "http://appgricultor.com/api/login")]
    public void VisualitiOptions_ResolvesUnifiedUrls(
        string apiUrl,
        string loginUrl,
        string expectedApi,
        string expectedLogin)
    {
        var opts = new VisualitiOptions
        {
            BaseUrl = "https://api.appgricultor.com",
            ApiUrl = apiUrl,
            LoginUrl = loginUrl,
        };
        opts.ResolveApiUrl().Should().Be(expectedApi);
        opts.ResolveLoginUrl().Should().Be(expectedLogin);
    }

    [Fact]
    public void ParseDevices_ReadsStationInventory()
    {
        const string json = """
        [
          {
            "origen_id": "4",
            "origen": "Red Inalámbrica de Sensores Visualiti",
            "estacion": "312",
            "name_device": "LIMA1 SUELO M312"
          },
          {
            "origen_id": "4",
            "origen": "Red Inalámbrica de Sensores Visualiti",
            "estacion": "333",
            "name_device": "SUELO M333"
          },
          {
            "origen_id": "4",
            "origen": "Red Inalámbrica de Sensores Visualiti",
            "estacion": "312",
            "name_device": "LIMA1 SUELO M312 DUP"
          }
        ]
        """;
        using var doc = JsonDocument.Parse(json);
        var devices = VisualitiClient.ParseDevices(doc.RootElement);
        devices.Should().HaveCount(2);
        devices[0].StationId.Should().Be(312);
        devices[0].Serial.Should().Be("M312");
        devices[0].DeviceName.Should().Be("LIMA1 SUELO M312");
        devices[0].OrigenId.Should().Be(4);
        devices[1].StationId.Should().Be(333);
        devices[1].Serial.Should().Be("M333");
    }

    [Theory]
    [InlineData("LIMA1 SUELO M312", "Lima")]
    [InlineData("AGUACATE2 SUELO M316", "Aguacate")]
    [InlineData("CACAO3 SUELO M320", "Cacao")]
    [InlineData("PAPAYA1 SUELO M321", "Papaya")]
    [InlineData("CHONTADURO CLIMA SUELO M240", null)]
    public void InferCrop_FromDeviceName(string name, string? expected)
    {
        VisualitiStationInventory.InferCrop(name).Should().Be(expected);
    }

    [Fact]
    public void ParseHardwareStatus_ReadsCoordsAndOnline()
    {
        const string json = """
        {
          "estacionVisualiti_id": "316",
          "estado_sensores": { "estado": "bueno" },
          "conectividad_estacion": { "estado": "online", "online": true },
          "coordenadas_estacion": { "latitud": "4.52249", "longitud": "-76.07792" },
          "payload": { "estacion": { "nombre": "SUELO M316" } }
        }
        """;
        using var doc = JsonDocument.Parse(json);
        var hw = VisualitiClient.ParseHardwareStatus(doc.RootElement, 316);
        hw.Should().NotBeNull();
        hw!.Latitude.Should().BeApproximately(4.52249, 0.0001);
        hw.Longitude.Should().BeApproximately(-76.07792, 0.0001);
        hw.Online.Should().BeTrue();
        hw.Connectivity.Should().Be("online");
    }

    [Theory]
    [InlineData(316, "America/Bogota")]
    [InlineData(333, "America/Guayaquil")]
    [InlineData(325, "America/Tegucigalpa")]
    public void CountryTimeZoneResolver_MapsStationIds(int stationId, string expected)
    {
        CountryTimeZoneResolver.ResolveForStation(stationId).Should().Be(expected);
    }

    [Theory]
    [InlineData(170, "America/Bogota")]
    [InlineData(218, "America/Guayaquil")]
    [InlineData(340, "America/Tegucigalpa")]
    public void CountryTimeZoneResolver_MapsCountryIds(int countryId, string expected)
    {
        CountryTimeZoneResolver.ResolveForCountryId(countryId).Should().Be(expected);
    }
}
