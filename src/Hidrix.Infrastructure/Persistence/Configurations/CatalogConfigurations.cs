using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Hidrix.Domain.Entities;

namespace Hidrix.Infrastructure.Persistence.Configurations;

/// <summary>Configuración EF de HidrtbRed.</summary>
public class HidrtbRedConfiguration : IEntityTypeConfiguration<HidrtbRed>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HidrtbRed> builder)
    {
        builder.ToTable("HidrtbRed", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.RedId);
        builder.Property(x => x.RedId).HasColumnName("Red_Id").ValueGeneratedOnAdd();
        builder.Property(x => x.RedNombre).HasColumnName("Red_Nombre").HasMaxLength(50).IsRequired();
        builder.Property(x => x.PaisId).HasColumnName("Pais_Id");
        builder.HasIndex(x => x.RedNombre).IsUnique();
        builder.HasOne(x => x.Pais).WithMany().HasForeignKey(x => x.PaisId);
    }
}

/// <summary>Configuración EF de HidrtbCultivo.</summary>
public class HidrtbCultivoConfiguration : IEntityTypeConfiguration<HidrtbCultivo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HidrtbCultivo> builder)
    {
        builder.ToTable("HidrtbCultivo", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.CultId);
        builder.Property(x => x.CultId).HasColumnName("Cult_Id").ValueGeneratedOnAdd();
        builder.Property(x => x.CultNombre).HasColumnName("Cult_Nombre").HasMaxLength(100).IsRequired();
        builder.Property(x => x.CultCapacidadCampo).HasColumnName("Cult_CapacidadCampo").HasPrecision(8, 2);
        builder.Property(x => x.CultPorcentajeMaximo).HasColumnName("Cult_PorcentajeMaximo").HasPrecision(8, 2);
        builder.Property(x => x.CultDecisionRiego).HasColumnName("Cult_DecisionRiego").HasPrecision(8, 2);
        builder.Property(x => x.CultActivo).HasColumnName("Cult_Activo");
        builder.Property(x => x.CultFechaCreacion).HasColumnName("Cult_FechaCreacion");
        builder.Property(x => x.CultFechaActualizacion).HasColumnName("Cult_FechaActualizacion");
        builder.HasIndex(x => x.CultNombre).IsUnique();
    }
}

/// <summary>Configuración EF de HidrtbSensor.</summary>
public class HidrtbSensorConfiguration : IEntityTypeConfiguration<HidrtbSensor>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HidrtbSensor> builder)
    {
        builder.ToTable("HidrtbSensor", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.SensId);
        builder.Property(x => x.SensId).HasColumnName("Sens_Id").ValueGeneratedOnAdd();
        builder.Property(x => x.SensNombre).HasColumnName("Sens_Nombre").HasMaxLength(50).IsRequired();
        builder.Property(x => x.RedId).HasColumnName("Red_Id");
        builder.Property(x => x.CultId).HasColumnName("Cult_Id");
        builder.Property(x => x.SensLatitud).HasColumnName("Sens_Latitud").HasPrecision(10, 7);
        builder.Property(x => x.SensLongitud).HasColumnName("Sens_Longitud").HasPrecision(10, 7);
        builder.Property(x => x.SensEstado).HasColumnName("Sens_Estado").HasMaxLength(50);
        builder.Property(x => x.SensConectividad).HasColumnName("Sens_Conectividad").HasMaxLength(20);
        builder.Property(x => x.SensFinca).HasColumnName("Sens_Finca").HasMaxLength(150);
        builder.Property(x => x.SensCanales).HasColumnName("Sens_Canales");
        builder.Property(x => x.SensActivo).HasColumnName("Sens_Activo");
        builder.Property(x => x.SensFechaCreacion).HasColumnName("Sens_FechaCreacion");
        builder.Property(x => x.SensFechaActualizacion).HasColumnName("Sens_FechaActualizacion");
        builder.Property(x => x.SensCcEstimado).HasColumnName("Sens_CCEstimado").HasPrecision(5, 2);
        builder.Property(x => x.SensMetodoCc).HasColumnName("Sens_MetodoCC").HasMaxLength(50);
        builder.Property(x => x.SensFechaEstimacionCc).HasColumnName("Sens_FechaEstimacionCC");
        builder.HasIndex(x => x.SensNombre).IsUnique();
        builder.HasOne(x => x.Red).WithMany(r => r.Sensores).HasForeignKey(x => x.RedId);
        builder.HasOne(x => x.Cultivo).WithMany(c => c.Sensores).HasForeignKey(x => x.CultId);
    }
}

/// <summary>Configuración EF de HidrtbMetodoCC.</summary>
public class HidrtbMetodoCCConfiguration : IEntityTypeConfiguration<HidrtbMetodoCC>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HidrtbMetodoCC> builder)
    {
        builder.ToTable("HidrtbMetodoCC", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.MetoId);
        builder.Property(x => x.MetoId).HasColumnName("Meto_Id").ValueGeneratedOnAdd();
        builder.Property(x => x.MetoNombre).HasColumnName("Meto_Nombre").HasMaxLength(120).IsRequired();
        builder.Property(x => x.MetoDescripcion).HasColumnName("Meto_Descripcion").HasMaxLength(500);
        builder.Property(x => x.MetoActivo).HasColumnName("Meto_Activo");
        builder.Property(x => x.MetoFechaCreacion).HasColumnName("Meto_FechaCreacion");
        builder.Property(x => x.MetoFechaActualizacion).HasColumnName("Meto_FechaActualizacion");
        builder.HasIndex(x => x.MetoNombre).IsUnique();
    }
}
