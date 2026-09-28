namespace Hidrix.Application.Common.Exceptions;

/// <summary>
/// Excepción de aplicación con código HTTP asociado.
/// </summary>
public class AppException : Exception
{
    /// <summary>Código de estado HTTP.</summary>
    public int StatusCode { get; }

    /// <summary>
    /// Crea una excepción de aplicación.
    /// </summary>
    /// <param name="message">Mensaje.</param>
    /// <param name="statusCode">Código HTTP (por defecto 400).</param>
    public AppException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}

/// <summary>
/// Recurso no encontrado (404).
/// </summary>
public class NotFoundException : AppException
{
    /// <summary>
    /// Crea una excepción 404.
    /// </summary>
    /// <param name="message">Mensaje.</param>
    public NotFoundException(string message) : base(message, 404)
    {
    }
}

/// <summary>
/// No autorizado (401).
/// </summary>
public class UnauthorizedAppException : AppException
{
    /// <summary>
    /// Crea una excepción 401.
    /// </summary>
    /// <param name="message">Mensaje.</param>
    public UnauthorizedAppException(string message) : base(message, 401)
    {
    }
}
