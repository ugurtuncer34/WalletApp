using System.ComponentModel.DataAnnotations;

namespace WalletBackend.Endpoints;

public class DtoValidationFilter<T> : IEndpointFilter
    where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // find the DTO in invocation arguments
        var dto = context.Arguments.OfType<T>().FirstOrDefault();
        if (dto is null)
            return Results.BadRequest("No payload.");

        // validate
        var validationCtx = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, validationCtx, results, true))
        {
            var errors = results
            .SelectMany(r => r.MemberNames
                .DefaultIfEmpty(string.Empty)
                .Select(name => new { name, msg = r.ErrorMessage! }))
            .GroupBy(x => x.name)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.msg).ToArray()
            );

            return Results.ValidationProblem(errors);
        }

        return await next(context);
    }
}