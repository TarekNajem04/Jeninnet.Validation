//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable CA1062

namespace Jeninnet.Validation.Sample.Mvc.Filters;

public sealed class ValidationFilter<T>(IValidator<T> validator) : IActionFilter where T : class {
    public void OnActionExecuting(ActionExecutingContext context) {
        if(context.ActionArguments.Values.FirstOrDefault(v => v is T) is not T model) {
            return;
        }

        var result = validator.Validate(model);
        if(result.IsValid) {
            return;
        }

        var errors = result.Errors
                           .GroupBy(e => e.MemberName)
                           .ToDictionary(g => g.Key, g => g.Select(e => e.Message ?? "Invalid").ToArray());

        context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors));
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
