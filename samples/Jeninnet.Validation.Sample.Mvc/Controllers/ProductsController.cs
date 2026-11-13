//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sample.Mvc.Controllers;

[ApiController]
[Route("api/[controller]")]
[ServiceFilter(typeof(ValidationFilter<ProductViewModel>))]
public sealed class ProductsController : ControllerBase {
    private static readonly List<ProductViewModel> _products = [];

    [HttpPost]
    public IActionResult Create([FromBody] ProductViewModel product) {
        ArgumentNullException.ThrowIfNull(product);
        _products.Add(product);
        return Ok(new { Id = Guid.NewGuid(), product.Name });
    }

    [HttpGet("self-test")]
    public IActionResult SelfTest([FromServices] IValidator<ProductViewModel> validator) {
        ArgumentNullException.ThrowIfNull(validator);
        var valid = new ProductViewModel(
            Name: "Laptop",
            Sku: "ELE-001",
            Price: 999.99m,
            Stock: 10,
            Category: "Electronics",
            Tags: ["portable", "new"]
        );

        var invalid = new ProductViewModel(
            Name: "",
            Sku: "bad-sku",
            Price: -1m,
            Stock: -5,
            Category: "Unknown", Tags: [""]
        );

        var validResult = validator.Validate(valid);
        var invalidResult = validator.Validate(invalid);

        return Ok(new {
            HasErrors = !validResult.IsValid,
            InvalidErrorCount = invalidResult.Errors.Count,
            Errors = invalidResult.Errors.Select(e => new {
                e.MemberName,
                e.Message,
                e.ErrorCode,
                e.Severity
            })
        });
    }
}
