//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure

Console.WriteLine("=== NESTED VALIDATION SHOWCASE ===");
Console.WriteLine();

// --- Level 1: Simple SetValidator ---
Console.WriteLine("--- 1. SetValidator ---");
RunSetValidatorDemo();

Console.WriteLine();

// --- Level 2: ChildRules (3 levels) ---
Console.WriteLine("--- 2. ChildRules (3 levels) ---");
RunChildRulesDemo();

Console.WriteLine();

// --- Level 3: Mixed SetValidator + ForEach ---
Console.WriteLine("--- 3. Mixed nesting (SetValidator + ForEach) ---");
RunMixedNestingDemo();

Console.WriteLine();

// --- Level 4: Deep nesting ---
Console.WriteLine("--- 4. Deep nesting (Company > Departments > Employees > Address > Country) ---");
RunDeepNestingDemo();

Console.WriteLine("Done.");

static void RunSetValidatorDemo() {
    var validator = new AddressValidators.CustomerValidator();
    var valid = new AddressModels.Customer("Alice", new AddressModels.Address("Main St", "Amsterdam", new AddressModels.Country("NL")));
    var invalid = new AddressModels.Customer("", new AddressModels.Address("", "", new AddressModels.Country("")));

    var validResult = validator.Validate(valid);
    Console.WriteLine($"Valid: {validResult.IsValid} (expected: True)");
    var invalidResult = validator.Validate(invalid);
    Console.WriteLine($"Invalid errors: {invalidResult.Errors.Count} (expected: 5)");
    foreach(var e in invalidResult.Errors) {
        Console.WriteLine($"  [{e.MemberName}] {e.Message}");
    }
}

static void RunChildRulesDemo() {
    var validator = new AddressValidators.OrderValidator();
    var invalid = new AddressModels.Order("ORD-001",
        new AddressModels.Customer("", new AddressModels.Address("", "", new AddressModels.Country(""))),
        [
            new AddressModels.OrderLine("", -1, -5m),
        ]);

    var invalidResult = validator.Validate(invalid);
    Console.WriteLine($"Errors: {invalidResult.Errors.Count} (expected: 6)");
    foreach(var e in invalidResult.Errors) {
        Console.WriteLine($"  [{e.MemberName}] {e.Message}");
    }
}

static void RunMixedNestingDemo() {
    var validator = new AddressValidators.InvoiceValidator();
    var invalid = new AddressModels.Invoice("", "USD",
    [
        new AddressModels.InvoiceLine("", 0, -1m),
        new AddressModels.InvoiceLine("Widget", 1, 10m),
        new AddressModels.InvoiceLine("", 2, -5m),
    ]);

    var invalidResult = validator.Validate(invalid);
    Console.WriteLine($"Errors: {invalidResult.Errors.Count} (expected: 6)");
    foreach(var e in invalidResult.Errors) {
        Console.WriteLine($"  [{e.MemberName}] {e.Message}");
    }
}

static void RunDeepNestingDemo() {
    var validator = new AddressValidators.CompanyValidator();
    var invalid = new AddressModels.Company("ACME",
    [
        new AddressModels.Department("Engineering",
        [
            new AddressModels.Employee("Bob",
                new AddressModels.Address("", "Utrecht", new AddressModels.Country("NL"))),
            new AddressModels.Employee("",
                new AddressModels.Address("Side St", "", new AddressModels.Country(""))),
        ]),
    ]);

    var invalidResult = validator.Validate(invalid);
    Console.WriteLine($"Errors: {invalidResult.Errors.Count} (expected: 5)");
    foreach(var e in invalidResult.Errors) {
        Console.WriteLine($"  [{e.MemberName}] {e.Message}");
    }
}

namespace AddressModels {
    file sealed record Country(string Code);
    file sealed record Address(string Street, string City, Country Country);
    file sealed record Customer(string Name, Address Address);
    file sealed record OrderLine(string ProductName, int Quantity, decimal UnitPrice);
    file sealed record Order(string Number, Customer Customer, List<OrderLine> Lines);
    file sealed record InvoiceLine(string ProductName, int Quantity, decimal UnitPrice);
    file sealed record Invoice(string Number, string Currency, List<InvoiceLine> Lines);
    file sealed record Employee(string Name, Address Address);
    file sealed record Department(string Name, List<Employee> Employees);
    file sealed record Company(string Name, List<Department> Departments);
}

namespace AddressValidators {
    file sealed class AddressValidator : Validator<AddressModels.Address> {
        protected override void Configure(ValidatorRuleBuilder<AddressModels.Address> builder) {
            builder.RuleFor(static a => a.Street, "Street").NotEmpty();
            builder.RuleFor(static a => a.City, "City").NotEmpty();
            builder.RuleFor(static a => a.Country, "Country").ChildRules(c =>
                c.RuleFor(static co => co.Code, "Code").NotEmpty().Length(2, 2));
        }
    }

    file sealed class CustomerValidator : Validator<AddressModels.Customer> {
        protected override void Configure(ValidatorRuleBuilder<AddressModels.Customer> builder) {
            builder.RuleFor(static c => c.Name, "Name").NotEmpty();
            builder.RuleFor(static c => c.Address, "Address").SetValidator(new AddressValidator());
        }
    }

    file sealed class OrderValidator : Validator<AddressModels.Order> {
        protected override void Configure(ValidatorRuleBuilder<AddressModels.Order> builder) {
            builder.RuleFor(static o => o.Number, "Number").NotEmpty();
            builder.RuleFor(static o => o.Customer, "Customer").ChildRules(customer => {
                customer.RuleFor(static c => c.Name, "Name").NotEmpty();
                customer.RuleFor(static c => c.Address, "Address").ChildRules(address => {
                    address.RuleFor(static a => a.Street, "Street").NotEmpty();
                    address.RuleFor(static a => a.City, "City").NotEmpty();
                });
            });
            builder.RuleFor(static o => o.Lines, "Lines")
                .ForEach(new OrderLineItemValidator())
                .ForEach(new OrderLineQuantityValidator())
                .ForEach(new OrderLinePriceValidator());
        }
    }

    file sealed class OrderLineItemValidator : Validator<AddressModels.OrderLine> {
        protected override void Configure(ValidatorRuleBuilder<AddressModels.OrderLine> builder) =>
            builder.RuleFor(static l => l.ProductName, "ProductName").NotEmpty();
    }

    file sealed class OrderLineQuantityValidator : Validator<AddressModels.OrderLine> {
        protected override void Configure(ValidatorRuleBuilder<AddressModels.OrderLine> builder) =>
            builder.RuleFor(static l => l.Quantity, "Quantity").GreaterThan(0);
    }

    file sealed class OrderLinePriceValidator : Validator<AddressModels.OrderLine> {
        protected override void Configure(ValidatorRuleBuilder<AddressModels.OrderLine> builder) =>
            builder.RuleFor(static l => l.UnitPrice, "UnitPrice").GreaterThan(0m);
    }

    file sealed class InvoiceLineValidator : Validator<AddressModels.InvoiceLine> {
        protected override void Configure(ValidatorRuleBuilder<AddressModels.InvoiceLine> builder) {
            builder.RuleFor(static l => l.ProductName, "ProductName").NotEmpty();
            builder.RuleFor(static l => l.Quantity, "Quantity").InclusiveBetween(1, 9999);
            builder.RuleFor(static l => l.UnitPrice, "UnitPrice").GreaterThan(0m);
        }
    }

    file sealed class InvoiceValidator : Validator<AddressModels.Invoice> {
        protected override void Configure(ValidatorRuleBuilder<AddressModels.Invoice> builder) {
            builder.RuleFor(static i => i.Number, "Number").NotEmpty();
            builder.RuleFor(static i => i.Currency, "Currency").NotEmpty().Length(3, 3);
            builder.RuleFor(static i => i.Lines, "Lines")
                .NotEmpty()
                .ForEach(new InvoiceLineValidator());
        }
    }

    file sealed class EmployeeValidator : Validator<AddressModels.Employee> {
        protected override void Configure(ValidatorRuleBuilder<AddressModels.Employee> builder) {
            builder.RuleFor(static e => e.Name, "Name").NotEmpty();
            builder.RuleFor(static e => e.Address, "Address").SetValidator(new AddressValidator());
        }
    }

    file sealed class DepartmentValidator : Validator<AddressModels.Department> {
        protected override void Configure(ValidatorRuleBuilder<AddressModels.Department> builder) {
            builder.RuleFor(static d => d.Name, "Name").NotEmpty();
            builder.RuleFor(static d => d.Employees, "Employees").ForEach(new EmployeeValidator());
        }
    }

    file sealed class CompanyValidator : Validator<AddressModels.Company> {
        protected override void Configure(ValidatorRuleBuilder<AddressModels.Company> builder) {
            builder.RuleFor(static c => c.Name, "Name").NotEmpty();
            builder.RuleFor(static c => c.Departments, "Departments").ForEach(new DepartmentValidator());
        }
    }
}
