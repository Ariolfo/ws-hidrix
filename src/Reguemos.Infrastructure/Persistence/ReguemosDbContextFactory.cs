using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Reguemos.Infrastructure.Persistence;

/// <summary>
/// Factory de diseño para EF Core CLI (migrations add / database update).
/// Permite ejecutar comandos EF sin arrancar Program.cs ni necesitar la BD activa.
/// Lee la cadena de conexión de la variable de entorno REGUEMOS_CONNECTION_STRING
/// o usa una cadena local por defecto.
/// </summary>
public class ReguemosDbContextFactory : IDesignTimeDbContextFactory<ReguemosDbContext>
{
    /// <inheritdoc />
    public ReguemosDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("REGUEMOS_CONNECTION_STRING")
            ?? "Server=localhost,1433;Database=dbReguemos;User Id=sa;Password=Hidrix_Str0ng!Passw0rd;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<ReguemosDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ReguemosDbContext(options);
    }
}
