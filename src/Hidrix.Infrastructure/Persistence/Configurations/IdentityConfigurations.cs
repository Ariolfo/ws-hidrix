using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Hidrix.Infrastructure.Identity;

namespace Hidrix.Infrastructure.Persistence.Configurations;

/// <summary>Configuración EF de ApplicationUser mapeada a HidrtbUsuario.</summary>
public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("HidrtbUsuario");
        builder.Property(u => u.Id).HasColumnName("Usua_Id");
        builder.Property(u => u.UsuaNombre).HasColumnName("Usua_Nombre").HasMaxLength(150).IsRequired();
        builder.Property(u => u.CiuId).HasColumnName("Ciu_Id");
        builder.Property(u => u.UsuaFechaRegistro).HasColumnName("Usua_FechaRegistro");
        builder.Property(u => u.UsuaFechaCreacion).HasColumnName("Usua_FechaCreacion");
        builder.Property(u => u.UsuaFechaActualizacion).HasColumnName("Usua_FechaActualizacion");
        builder.Property(u => u.UsuaActivo).HasColumnName("Usua_Activo");
        builder.HasOne(u => u.Ciudad).WithMany().HasForeignKey(u => u.CiuId).IsRequired(false);
    }
}
