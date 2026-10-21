using System.Reflection;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Reguemos.Application.Common.Behaviors;

namespace Reguemos.Application;

/// <summary>
/// Registro de dependencias de la capa Application.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Agrega FluentValidation y behaviours de Application.
    /// Mediator se registra en Reguemos.Api (el source generator solo corre en el proyecto edge).
    /// </summary>
    /// <param name="services">Contenedor DI.</param>
    /// <returns>El mismo contenedor.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }
}
