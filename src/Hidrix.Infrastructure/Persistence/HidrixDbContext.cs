using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Hidrix.Domain.Entities;
using Hidrix.Infrastructure.Identity;

namespace Hidrix.Infrastructure.Persistence;

/// <summary>
/// Contexto EF Core con soporte de Identity. Todas las tablas usan prefijo Hidrtb.
/// </summary>
public class HidrixDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    /// <summary>Inicializa el contexto.</summary>
    public HidrixDbContext(DbContextOptions<HidrixDbContext> options) : base(options)
    {
    }

    /// <summary>Países.</summary>
    public DbSet<HidrtbPais> Paises => Set<HidrtbPais>();

    /// <summary>Departamentos.</summary>
    public DbSet<HidrtbDepartamento> Departamentos => Set<HidrtbDepartamento>();

    /// <summary>Ciudades.</summary>
    public DbSet<HidrtbCiudad> Ciudades => Set<HidrtbCiudad>();

    /// <summary>Redes de sensores.</summary>
    public DbSet<HidrtbRed> Redes => Set<HidrtbRed>();

    /// <summary>Cultivos.</summary>
    public DbSet<HidrtbCultivo> Cultivos => Set<HidrtbCultivo>();

    /// <summary>Metadatos de sensores Visualiti (cultivo / CC).</summary>
    public DbSet<HidrtbSensorMeta> SensorMetas => Set<HidrtbSensorMeta>();

    /// <summary>Métodos para capacidad de campo.</summary>
    public DbSet<HidrtbMetodoCC> MetodosCC => Set<HidrtbMetodoCC>();

    /// <summary>Registros de la calculadora de riego.</summary>
    public DbSet<HidrtbCalculoRiego> CalculosRiego => Set<HidrtbCalculoRiego>();

    /// <summary>Notas de evento de riego.</summary>
    public DbSet<HidrtbNotaEventoRiego> NotasEventoRiego => Set<HidrtbNotaEventoRiego>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity configures its entities first
        base.OnModelCreating(modelBuilder);

        // Apply our EF configurations (picks up ApplicationUserConfiguration, etc.)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HidrixDbContext).Assembly);

        // Rename remaining Identity tables to Hidrtb prefix
        modelBuilder.Entity<IdentityRole>().ToTable("HidrtbRol");
        modelBuilder.Entity<IdentityUserRole<string>>().ToTable("HidrtbUsuarioRol");
        modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("HidrtbUsuarioClaim");
        modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("HidrtbUsuarioLogin");
        modelBuilder.Entity<IdentityUserToken<string>>().ToTable("HidrtbUsuarioToken");
        modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("HidrtbRolClaim");
    }
}
