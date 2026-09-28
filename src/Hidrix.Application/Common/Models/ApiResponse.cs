namespace Hidrix.Application.Common.Models;

/// <summary>
/// Sobre uniforme de respuesta para todos los endpoints de la API.
/// </summary>
/// <typeparam name="T">Tipo del payload en <see cref="Data"/>.</typeparam>
public class ApiResponse<T>
{
    /// <summary>Indica si la operación fue exitosa.</summary>
    public bool Success { get; set; }

    /// <summary>Mensaje descriptivo para el cliente.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Datos de la respuesta o null si hubo error.</summary>
    public T? Data { get; set; }

    /// <summary>
    /// Crea una respuesta exitosa.
    /// </summary>
    /// <param name="data">Payload.</param>
    /// <param name="message">Mensaje opcional.</param>
    /// <returns>Instancia con Success = true.</returns>
    public static ApiResponse<T> Ok(T data, string message = "OK") =>
        new() { Success = true, Message = message, Data = data };

    /// <summary>
    /// Crea una respuesta de error.
    /// </summary>
    /// <param name="message">Mensaje de error.</param>
    /// <returns>Instancia con Success = false y Data = null.</returns>
    public static ApiResponse<T> Fail(string message) =>
        new() { Success = false, Message = message, Data = default };
}
