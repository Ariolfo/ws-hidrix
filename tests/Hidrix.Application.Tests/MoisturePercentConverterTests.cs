using FluentAssertions;
using Hidrix.Application.Services;

namespace Hidrix.Application.Tests;

/// <summary>
/// Pruebas de conversión de humedad Visualiti a porcentaje.
/// </summary>
public class MoisturePercentConverterTests
{
    [Theory]
    [InlineData(0.82, 82.0)]
    [InlineData(1.5, 150.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(35.2, 35.2)]
    [InlineData(1.51, 1.51)]
    public void ToPercent_ConvertsFractionOrKeepsPercent(double input, double expected)
    {
        MoisturePercentConverter.ToPercent(input).Should().Be(expected);
    }
}
