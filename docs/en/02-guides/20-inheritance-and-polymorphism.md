# Inheritance and Polymorphism

Validation often crosses an inheritance boundary: a collection is typed as a base class or an
interface, while each element has its own runtime type. Jeninnet.Validation keeps that dispatch
explicit in both authoring paths.

## Generated validators inherit base rules

Annotate every type that should have its own generated validator. When a derived type is annotated,
the generator walks its base classes and emits the base property rules first, followed by the
derived rules:

```csharp
[Validate]
public class Contact
{
    [NotEmpty] public string? Name { get; set; }
}

[Validate]
public sealed class Person : Contact
{
    [NotEmpty] public string? Email { get; set; }
}
```

`PersonValidator` contains both the `Name` and `Email` rules. Overridden properties contribute the
base rules once; an attribute repeated on the override is deduplicated.

A base type with `[Validate]` does **not** silently generate validators for its derived types. This
is deliberate: generation stays explicit, so adding a derived model never changes the set of
generated types by accident.

## Interfaces as rule contracts

`[Validate]` can also decorate an interface. A class that implements an annotated interface and is
itself annotated inherits the matching interface property rules:

```csharp
[Validate]
public interface IContact
{
    [NotEmpty] string Name { get; }
}

[Validate]
public sealed class Customer : IContact
{
    public string Name { get; set; } = string.Empty;
}
```

Only interfaces explicitly annotated with `[Validate]` contribute rules. Interface members are
matched to public class members by name and type; explicit interface implementations are not
matched.

## Compose a base validator in fluent code

`IncludeBase` composes a validator declared for a base type or interface. The base validator
receives the derived instance and the same validation context:

```csharp
public sealed class PersonValidator : Validator<Person>
{
    protected override void Configure(ValidatorRuleBuilder<Person> builder)
    {
        builder.IncludeBase<Person, Contact>(new ContactValidator());
        builder.RuleFor(x => x.Email, nameof(Person.Email)).NotEmpty();
    }
}
```

`Include` stays the same-type composition mechanism. `IncludeBase` is the cross-type equivalent;
both preserve `Root`, `RootData`, services, rule sets, fail-fast, and error metadata.

## Dispatch by runtime type

Use `SetInheritanceValidator` when a property is declared as a base type or interface but each
concrete type has its own validator:

```csharp
builder.RuleFor(x => x.Primary, nameof(Container.Primary))
       .SetInheritanceValidator(map =>
       {
           map.Add<Person>(new PersonValidator());
           map.Add<Organisation>(new OrganisationValidator());
       });
```

The map is explicit and fixed at configuration time. Dispatch is a dictionary lookup on
`value.GetType()`; there is no reflection or `Activator` call in the validation path.

Only the exact runtime type is resolved. A runtime type that is not registered is skipped, matching
the behavior of an unconfigured optional rule. Register every concrete type that needs validation;
do not rely on a base-type fallback. `InheritanceValidatorMap<T>.Add` rejects duplicate registrations.

## Polymorphic collections

Every `ForEach` collection shape has an inheritance-map overload:

```csharp
builder.RuleFor(x => x.Contacts, nameof(Container.Contacts))
       .ForEach(map =>
       {
           map.Add<Person>(new PersonValidator());
           map.Add<Organisation>(new OrganisationValidator());
       });
```

Array, `IEnumerable<T>`, `ICollection<T>`, `IList<T>`, and `List<T>` members are supported. Error
paths keep the element index and the concrete member path:

```text
Contacts[0].Email
Contacts[1].Registration
```

The same dispatcher works on the asynchronous path. An asynchronous validator registered in the map
is awaited by `ValidateAsync`; synchronous validators still run on the asynchronous path.

## Generated containers and polymorphic elements

The generated `[ForEach]` rule injects an `IValidator<TElement>`. Pass an `InheritanceValidator<T>`
for a polymorphic element type:

```csharp
public sealed class ContactInheritanceValidator : Validator<Contact>
{
    protected override void Configure(ValidatorRuleBuilder<Contact> builder)
    {
        var map = new InheritanceValidatorMap<Contact>();
        map.Add<Person>(new PersonValidator());
        map.Add<Organisation>(new OrganisationValidator());

        builder.Include(new InheritanceValidator<Contact>(map));
    }
}

var containerValidator = new ContactContainerValidator(new ContactInheritanceValidator());
```

This keeps the generated container independent of concrete element types while the runtime
dispatcher chooses the right validator.

## Sync and async behavior

- `IValidator<T>` entries support both paths.
- Sync-only entries work on both paths; the asynchronous path executes their synchronous rules.
- Async-only entries throw `InvalidOperationException` on the synchronous path instead of silently
  skipping rules. Use `ValidateAsync` for those validators.
- Context-aware validators preserve the parent member path; plain validators are prefixed exactly
  once by the nested or collection rule.

## Related

- [Fluent authoring](01-fluent-authoring.md)
- [Attribute authoring](02-attribute-authoring.md)
- [Collections and nesting](06-collections-and-nesting.md)
- [Known limitations](../03-reference/07-known-limitations.md)
