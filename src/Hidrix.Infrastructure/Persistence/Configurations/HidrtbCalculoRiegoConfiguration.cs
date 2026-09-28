using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Hidrix.Domain.Entities;

namespace Hidrix.Infrastructure.Persistence.Configurations;

/// <summary>Configuración EF de HidrtbCalculoRiego.</summary>
public class HidrtbCalculoRiegoConfiguration : IEntityTypeConfiguration<HidrtbCalculoRiego>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HidrtbCalculoRiego> builder)
    {
        builder.ToTable("HidrtbCalculoRiego", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.CalcId);
        builder.Property(x => x.CalcId).HasColumnName("Calc_Id").ValueGeneratedOnAdd();
        builder.Property(x => x.UsuaId).HasColumnName("Usua_Id").HasMaxLength(450).IsRequired();
        builder.Property(x => x.CalcCultivoNombre).HasColumnName("Calc_CultivoNombre").HasMaxLength(100).IsRequired();
        builder.Property(x => x.CultId).HasColumnName("Cult_Id");
        builder.Property(x => x.CalcCapacidadCampo).HasColumnName("Calc_CapacidadCampo").HasPrecision(8, 2);
        builder.Property(x => x.CalcLimiteMaxRiego).HasColumnName("Calc_LimiteMaxRiego").HasPrecision(8, 2);
        builder.Property(x => x.CalcDecisionRiego).HasColumnName("Calc_DecisionRiego").HasPrecision(8, 2);
        builder.Property(x => x.CalcFechaConsulta).HasColumnName("Calc_FechaConsulta");
        builder.Property(x => x.CalcHumedadManana).HasColumnName("Calc_HumedadManana").HasPrecision(8, 2);
        builder.Property(x => x.CalcHumedadTarde).HasColumnName("Calc_HumedadTarde").HasPrecision(8, 2);
        builder.Property(x => x.CalcRecomendacion).HasColumnName("Calc_Recomendacion").HasMaxLength(20).IsRequired();
        builder.Property(x => x.CalcRealizoRiego).HasColumnName("Calc_RealizoRiego").HasMaxLength(10);
        builder.Property(x => x.CalcObservacion).HasColumnName("Calc_Observacion").HasMaxLength(500);
        builder.Property(x => x.CalcActivo).HasColumnName("Calc_Activo");
        builder.Property(x => x.CalcFechaCreacion).HasColumnName("Calc_FechaCreacion");
        builder.Property(x => x.CalcFechaActualizacion).HasColumnName("Calc_FechaActualizacion");
        builder.HasIndex(x => new { x.UsuaId, x.CalcCultivoNombre });
    }
}
