using Hidrix.Application.Common.Interfaces;
using Hidrix.Application.DTOs;
using Hidrix.Application.Services;

namespace Hidrix.Application.Services;

/// <summary>
/// Mapeo de sensores del catálogo a DTOs de API.
/// </summary>
public static class SensorMapper
{
    private static readonly Dictionary<int, int> ChannelDepthCm = new() { { 1, 10 }, { 2, 30 } };

    /// <summary>
    /// Formatea la ubicación textual del sensor.
    /// </summary>
    public static string FormatLocation(PhysicalSensor sensor)
    {
        if (!string.IsNullOrWhiteSpace(sensor.Pais))
        {
            return sensor.Pais;
        }

        var finca = (sensor.Finca ?? string.Empty).Trim();
        return string.IsNullOrEmpty(finca) ? "Ubicación no disponible" : finca;
    }

    /// <summary>
    /// Formatea el nombre del sensor físico.
    /// </summary>
    public static string FormatSensorName(PhysicalSensor sensor)
    {
        var cultivo = (sensor.Cultivo ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(cultivo) && cultivo is not ("No aplica" or "Todos"))
        {
            return $"Sensor {sensor.Serial} – {cultivo}";
        }

        return $"Sensor {sensor.Serial}";
    }

    /// <summary>
    /// Construye lecturas DTO desde Visualiti.
    /// </summary>
    public static List<ReadingDto> ReadingsFrom(VisualitiReading? reading)
    {
        if (reading is null)
        {
            return [];
        }

        var result = new List<ReadingDto>();
        foreach (var channel in reading.Channels)
        {
            if (!ChannelDepthCm.TryGetValue(channel, out var depth))
            {
                continue;
            }

            result.Add(new ReadingDto
            {
                DepthCm = depth,
                Value = reading.Valores[channel],
                Timestamp = reading.FechaHora,
                Unit = "%",
            });
        }

        return result;
    }

    /// <summary>
    /// Arma la respuesta del sensor físico o lógico.
    /// </summary>
    public static SensorDto ToSensorDto(
        PhysicalSensor sensor,
        string stationId,
        VisualitiReading? reading,
        int? channel = null)
    {
        string sensorId;
        string name;
        double? value;
        List<ReadingDto> readings;

        if (channel is not null)
        {
            sensorId = SensorCatalog.LogicalSensorId(sensor.Serial, channel.Value);
            value = reading?.ValueForChannel(channel.Value);
            readings = ReadingsFrom(reading)
                .Where(r => r.DepthCm == ChannelDepthCm.GetValueOrDefault(channel.Value))
                .ToList();
            name = $"Sensor {sensorId}";
            if (!string.IsNullOrWhiteSpace(sensor.Cultivo) &&
                sensor.Cultivo is not ("No aplica" or "Todos"))
            {
                name += $" – {sensor.Cultivo}";
            }
        }
        else
        {
            sensorId = sensor.Serial;
            value = reading?.Volumetrico1;
            readings = ReadingsFrom(reading);
            name = FormatSensorName(sensor);
        }

        var (status, alert) = MoistureStatusInterpreter.Interpret(value, sensor.Cultivo);

        return new SensorDto
        {
            Id = sensorId,
            StationId = stationId,
            Name = name,
            Location = FormatLocation(sensor),
            Status = status,
            Connectivity = sensor.Connectivity,
            Online = sensor.Online,
            HardwareStatus = sensor.HardwareStatus,
            LastReadingAt = reading?.FechaHora ?? DateTimeOffset.UtcNow,
            Readings = readings,
            AlertMessage = alert,
            Latitude = sensor.Latitud,
            Longitude = sensor.Longitud,
            TimeZoneId = sensor.TimeZoneId,
        };
    }

    /// <summary>
    /// Expande un sensor físico en sus canales lógicos (M###-1, M###-2, …).
    /// </summary>
    public static IReadOnlyList<SensorDto> ToLogicalSensorDtos(
        PhysicalSensor sensor,
        string stationId,
        VisualitiReading? reading,
        IReadOnlyList<int>? channels = null)
    {
        var channelList = channels;
        IEnumerable<int> sequence;
        if (channelList is { Count: > 0 })
        {
            sequence = channelList;
        }
        else if (sensor.Canales > 0)
        {
            sequence = Enumerable.Range(1, sensor.Canales);
        }
        else
        {
            sequence = Enumerable.Range(1, SensorCatalog.DefaultChannels);
        }

        return sequence.Select(ch => ToSensorDto(sensor, stationId, reading, ch)).ToList();
    }
}
