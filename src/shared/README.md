# Shared source

This folder follows the sibling `Jeninnet.Localization` shared-source pattern:

- each shared unit lives in its own folder with a `.projitems` file;
- consuming projects import the `.projitems` and compile the source into their own
  assembly (no shared runtime assembly and no package dependency);
- namespaces are stable and internal types stay internal to each consumer.

Current units:

| Unit | Consumers | Purpose |
|---|---|---|
| `JsonValueFactory` | `Jeninnet.Validation.JsonSchema`, `Jeninnet.Validation.ClientRules` | Converts descriptor parameter primitives to `JsonNode` values without reflection. |

This directory is repository infrastructure, not a NuGet package. Do not reference it from
consumer-facing documentation; link to the packages instead.
