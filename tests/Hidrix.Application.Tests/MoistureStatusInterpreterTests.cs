using FluentAssertions;
using Hidrix.Application.Services;

namespace Hidrix.Application.Tests;

/// <summary>
/// Pruebas de interpretación de estado por CC del cultivo (3 zonas).
/// </summary>
public class MoistureStatusInterpreterTests
{
    [Fact]
    public void Interpret_Null_IsNoData()
    {
        var (status, alert) = MoistureStatusInterpreter.Interpret(null, "Cacao");
        status.Should().Be("no_data");
        alert.Should().BeNull();
    }

    [Theory]
    [InlineData(21.76, "normal")]
    [InlineData(25.0, "normal")]
    [InlineData(27.2, "normal")]
    [InlineData(27.21, "drain")]
    [InlineData(34.0, "drain")]
    [InlineData(48.0, "drain")]
    [InlineData(21.75, "irrigate_deficit")]
    [InlineData(15.0, "irrigate_deficit")]
    [InlineData(5.0, "irrigate_deficit")]
    public void Interpret_Cacao_ThreeZones(double value, string expected)
    {
        var (status, _) = MoistureStatusInterpreter.Interpret(value, "Cacao");
        status.Should().Be(expected);
    }

    [Fact]
    public void Interpret_Cacao_NormalMessage()
    {
        var (_, alert) = MoistureStatusInterpreter.Interpret(25.0, "Cacao");
        alert.Should().Be("Normal, No regar");
    }

    [Fact]
    public void Interpret_Cacao_DrainMessage()
    {
        var (_, alert) = MoistureStatusInterpreter.Interpret(30.0, "Cacao");
        alert.Should().Be("Alerta Drenar - saturación");
    }

    [Fact]
    public void Interpret_Cacao_DeficitMessage()
    {
        var (_, alert) = MoistureStatusInterpreter.Interpret(15.0, "Cacao");
        alert.Should().Be("Alerta REGAR por déficit");
    }

    [Fact]
    public void Interpret_Lima_30_IsDrain()
    {
        var (status, _) = MoistureStatusInterpreter.Interpret(30, "Lima ácida Tahiti");
        status.Should().Be("drain");
    }

    [Fact]
    public void Interpret_Lima_25_IsNormal()
    {
        var (status, _) = MoistureStatusInterpreter.Interpret(25, "Lima ácida Tahiti");
        status.Should().Be("normal");
    }

    [Fact]
    public void Resolve_LimaAcidaTahiti_UsesLimaProfile()
    {
        CropMoistureProfiles.Resolve("Lima ácida Tahiti").Name.Should().Be("Lima");
    }
}
