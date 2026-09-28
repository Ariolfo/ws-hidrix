using FluentValidation;
using Mediator;

namespace Hidrix.Application.Common.Behaviors;

/// <summary>
/// Pipeline de Mediator que ejecuta validadores FluentValidation antes del handler.
/// </summary>
/// <typeparam name="TRequest">Tipo de solicitud.</typeparam>
/// <typeparam name="TResponse">Tipo de respuesta.</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IMessage
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Inicializa el comportamiento de validación.
    /// </summary>
    /// <param name="validators">Validadores registrados para la solicitud.</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <summary>
    /// Ejecuta la validación y continúa el pipeline.
    /// </summary>
    /// <param name="request">Solicitud.</param>
    /// <param name="next">Delegado siguiente.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Respuesta del handler.</returns>
    public async ValueTask<TResponse> Handle(
        TRequest request,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next(request, cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        return await next(request, cancellationToken);
    }
}
