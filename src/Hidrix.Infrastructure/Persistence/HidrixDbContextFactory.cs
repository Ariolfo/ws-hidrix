using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hidrix.Infrastructure.Persistence;

/// <summary>
/// Factory de diseño para EF Core CLI (migrations add / database update).
/// Permite ejecutar comandos EF sin arrancar Program.cs ni necesitar la BD activa.
/// Lee la cadena de conexión de la variable de entorno HIDRIX_CONNECTION_STRING
/// o usa una cadena local por defecto.
/// </summary>
public class HidrixDbContextFactory : IDesignTimeDbContextFactory<HidrixDbContext>
{
    /// <inheritdoc />
    public HidrixDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("HIDRIX_CONNECTION_STRING")
            ?? "Server=localhost,1433;Database=dbHidrix;User Id=sa;Password=Hidrix_Str0ng!Passw0rd;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<HidrixDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new HidrixDbContext(options);
    }
}
