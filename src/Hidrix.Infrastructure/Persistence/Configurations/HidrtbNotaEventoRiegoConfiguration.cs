using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Hidrix.Domain.Entities;

namespace Hidrix.Infrastructure.Persistence.Configurations;

/// <summary>Configuración EF de HidrtbNotaEventoRiego.</summary>
public class HidrtbNotaEventoRiegoConfiguration : IEntityTypeConfiguration<HidrtbNotaEventoRiego>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HidrtbNotaEventoRiego> builder)
    {
        builder.ToTable("HidrtbNotaEventoRiego", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.EvriId);
        builder.Property(x => x.EvriId).HasColumnName("Evri_Id").ValueGeneratedOnAdd();
        builder.Property(x => x.UsuaId).HasColumnName("Usua_Id").HasMaxLength(450).IsRequired();
        builder.Property(x => x.EvriNombreParcelaLote).HasColumnName("Evri_NombreParcelaLote").HasMaxLength(120).IsRequired();
        builder.Property(x => x.EvriCultivo).HasColumnName("Evri_Cultivo").HasMaxLength(100).IsRequired();
        builder.Property(x => x.CultId).HasColumnName("Cult_Id");
        builder.Property(x => x.EvriFecha).HasColumnName("Evri_Fecha");
        builder.Property(x => x.EvriHoraInicio).HasColumnName("Evri_HoraInicio");
        builder.Property(x => x.EvriHoraFin).HasColumnName("Evri_HoraFin");
        builder.Property(x => x.EvriDuracionMinutos).HasColumnName("Evri_DuracionMinutos");
        builder.Property(x => x.EvriTipoRiego).HasColumnName("Evri_TipoRiego").HasMaxLength(30).IsRequired();
        builder.Property(x => x.EvriCaudalHoraLph).HasColumnName("Evri_CaudalHoraLph").HasPrecision(10, 2);
        builder.Property(x => x.EvriTipoSuelo).HasColumnName("Evri_TipoSuelo").HasMaxLength(80);
        builder.Property(x => x.CiuId).HasColumnName("Ciu_Id");
        builder.HasOne(x => x.Ciudad).WithMany().HasForeignKey(x => x.CiuId).IsRequired(false);
        builder.Property(x => x.EvriLugarLatitud).HasColumnName("Evri_LugarLatitud").HasPrecision(9, 6);
        builder.Property(x => x.EvriLugarLongitud).HasColumnName("Evri_LugarLongitud").HasPrecision(9, 6);
        builder.Property(x => x.EvriActivo).HasColumnName("Evri_Activo");
        builder.Property(x => x.EvriFechaCreacion).HasColumnName("Evri_FechaCreacion");
        builder.Property(x => x.EvriFechaActualizacion).HasColumnName("Evri_FechaActualizacion");
        builder.HasIndex(x => new { x.UsuaId, x.EvriFecha });
    }
}
