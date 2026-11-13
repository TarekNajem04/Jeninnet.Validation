# Collections and Nesting

## Cardinality

```csharp
builder.RuleFor(x => x.Tags, nameof(Order.Tags)).Count(1, 5);
builder.RuleFor(x => x.Lines, nameof(Order.Lines)).MinCount(1).MaxCount(100);
```

Count rules accept any `IEnumerable` (arrays, lists, sets, sequences). `null` is a no-op. The rules
enumerate once unless the source exposes `ICollection.Count`.

Attribute equivalents: `[Count]`, `[MinCount]`, `[MaxCount]`.

## Validate each element

```csharp
builder.RuleFor(x => x.Lines, nameof(Order.Lines))
       .ForEach(new OrderLineValidator());
```

Inline child rules:

```csharp
builder.RuleFor(x => x.Lines, nameof(Order.Lines))
       .ForEach(line => line.RuleFor(x => x.Quantity, nameof(OrderLine.Quantity)).GreaterThan(0));
```

Error paths use 0-based indexes: `Lines[0].Quantity`.

## Element value rules

`ForEachElement` applies ordinary value rules directly to each element, without a child
validator:

```csharp
builder.RuleFor(x => x.Tags, nameof(Order.Tags))
       .ForEachElement(element => element.NotEmpty());

builder.RuleFor(x => x.Values, nameof(Order.Values))
       .ForEachElement(element => element.NotNull());
```

Element rules run for `null` elements, so `NotNull` reports a missing entry instead of skipping
it. The path is the collection member plus the element index (`Tags[0]`). Array,
`IEnumerable<T>`, `ICollection<T>`, `IList<T>`, and `List<T>` members are supported.

`Where` and `OverrideIndexer` work with both `ForEach` and `ForEachElement`:

```csharp
builder.RuleFor(x => x.Lines, nameof(Order.Lines))
       .ForEachElement(line => line.Must(l => l.Quantity > 0))
       .Where(line => line.IsActive)
       .OverrideIndexer((_, index) => $"id-{index}");
```

`Where` preserves the original element indexes. `OverrideIndexer` changes only the displayed
indexer; `CollectionIndex` on the error keeps the original integer index. Element errors expose it
as a message parameter, so a custom message pack can use `{CollectionIndex}`.

## Path filtering

`IncludeProperties` and `ExcludeProperties` accept collection and nested sub-paths. Both `[]` and
`[*]` are accepted; numeric indexes are normalized because filtering applies to all elements:

```csharp
validator.Validate(model, new ValidationCallOptions {
    IncludeProperties = ["Lines[].Quantity"]
});

validator.Validate(model, new ValidationCallOptions {
    ExcludeProperties = ["Lines[*].Name"]
});
```

Sub-paths flow through nested (`SetValidator`), collection (`ForEach`), and inline (`ChildRules`)
validators. Filtering does not support index-specific selection (`Lines[2].Quantity` selects all
lines). Custom indexers produced by `OverrideIndexer` are treated as the collection segment by the
path filter.

## Polymorphic elements

When the element type is a base class or interface, use the inheritance-map overload to choose a
validator from each element's runtime type:

```csharp
builder.RuleFor(x => x.Contacts, nameof(Container.Contacts))
       .ForEach(map =>
       {
           map.Add<Person>(new PersonValidator());
           map.Add<Organisation>(new OrganisationValidator());
       });
```

Unknown element types are skipped. Array, `IEnumerable<T>`, `ICollection<T>`, `IList<T>`, and
`List<T>` members are supported. See
[Inheritance and polymorphism](20-inheritance-and-polymorphism.md).

## Element filters

```csharp
builder.RuleFor(x => x.Lines, nameof(Order.Lines))
       .ForEach(new OrderLineValidator())
       .Where(line => line.IsActive);
```

`WhereAsync` is async-path-only. When both filters are present, both must pass. Original indexes are
preserved, so filtered-out elements do not shift error paths.

## Conditions on collections

`.When(...)` on a collection rule is evaluated against the parent instance, not per element:

```csharp
builder.RuleFor(x => x.Lines, nameof(Order.Lines))
       .ForEach(new OrderLineValidator())
       .Where(line => line.IsActive)
       .When(order => order.ValidateLines);
```

The condition gates both the synchronous and asynchronous paths. Element errors keep the item path
(`Lines[2].Quantity`) or the `OverrideIndexer` text even when the condition and filter are combined;
`CollectionIndex` still reports the original integer index. For a per-element condition, place
`.When(...)` inside the child validator instead.

## Nested objects

Reusable validator:

```csharp
builder.RuleFor(x => x.Address, nameof(Customer.Address))
       .SetValidator(new AddressValidator());
```

Inline child rules:

```csharp
builder.RuleFor(x => x.Address, nameof(Customer.Address))
       .ChildRules(address =>
       {
           address.RuleFor(a => a.Street, nameof(Address.Street)).NotEmpty();
           address.RuleFor(a => a.City, nameof(Address.City)).NotEmpty();
       });
```

Deep paths compose: `Customer.Address.Street`, `Order.Lines[0].Quantity`.

Nested, collection, and included validators inherit the parent context: `Root`, the shared
`RootData` bag, `ServiceProvider`, `RuleSets`, `FailFast`, and the current `MemberPath`. Error
metadata (`MessageKey`, parameters, `ErrorCode`, `Severity`, `CustomState`) is preserved across
the boundary, and relative paths emitted by custom rules are prefixed once.

## Null collections and null children

`ForEach` skips null collections. `SetValidator` and `ChildRules` skip null children; combine with
`NotNull` when the child itself is required.

## Related

- [Fluent authoring](01-fluent-authoring.md)
- [Rule reference](../03-reference/02-rule-reference.md)
