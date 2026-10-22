using FluentAssertions;
using Reguemos.Application.Services;
using Reguemos.Infrastructure.Services;

namespace Reguemos.Application.Tests;

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
            new Reguemos.Application.Common.Interfaces.VisualitiReading
            {
                FechaHora = DateTimeOffset.UtcNow.AddHours(-1),
                Valores = new Dictionary<int, double> { [1] = 28 },
            },
            new Reguemos.Application.Common.Interfaces.VisualitiReading
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

    [Fact]
    public async Task RunSingleFlightAsync_SharesOneFactoryCall()
    {
        var cache = new VisualitiMoistureCache();
        var calls = 0;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<int> Factory(CancellationToken _)
        {
            Interlocked.Increment(ref calls);
            await gate.Task;
            return 54;
        }

        var first = cache.RunSingleFlightAsync("visualiti-data:54", Factory);
        var second = cache.RunSingleFlightAsync("visualiti-data:54", Factory);

        await Task.Delay(50);
        gate.SetResult();

        var results = await Task.WhenAll(first, second);
        calls.Should().Be(1);
        results.Should().BeEquivalentTo(new[] { 54, 54 });
    }
}
