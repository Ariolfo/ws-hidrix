using FluentAssertions;
using Hidrix.Application.Services;
using Hidrix.Infrastructure.Services;

namespace Hidrix.Application.Tests;

/// <summary>
/// Pruebas de rangos de histórico y caché Visualiti en memoria.
/// </summary>
public class HistoryRangeAndCacheTests
{
    [Theory]
    [InlineData("7d", true)]
    [InlineData("30d", true)]
    [InlineData("6m", true)]
    [InlineData("today", true)]
    [InlineData("1y", false)]
    public void HistoryRangeHelper_ValidatesKeys(string key, bool expected)
    {
        HistoryRangeHelper.IsValid(key).Should().Be(expected);
    }

    [Fact]
    public void VisualitiMoistureCache_StoresAndExpiresHistoryIndependentlyOfClientInstance()
    {
        var cache = new VisualitiMoistureCache();
        var readings = new[]
        {
            new Hidrix.Application.Common.Interfaces.VisualitiReading
            {
                FechaHora = DateTimeOffset.UtcNow.AddHours(-1),
                Valores = new Dictionary<int, double> { [1] = 28 },
            },
            new Hidrix.Application.Common.Interfaces.VisualitiReading
            {
                FechaHora = DateTimeOffset.UtcNow,
                Valores = new Dictionary<int, double> { [1] = 30 },
            },
        };

        cache.SetHistory("M314", "7d", readings, TimeSpan.FromMinutes(5));
        cache.TryGetHistory("M314", "7d", out var cached).Should().BeTrue();
        cached.Should().HaveCount(2);
        cache.FindLatestFromHistory("M314")!.Valores[1].Should().Be(30);
    }
}
