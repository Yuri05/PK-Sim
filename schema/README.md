# PK-Sim snapshot JSON schema

[`PK-Sim-Snapshot.schema.json`](PK-Sim-Snapshot.schema.json) is a [JSON Schema](https://json-schema.org/)
(draft-07) that describes the structure of PK-Sim **project snapshots** — the `*.json` files that PK-Sim
imports and exports (for example the published Open Systems Pharmacology model and DDI snapshots).

## How it was derived

The schema mirrors the snapshot object model and its serialization configuration:

- Snapshot classes in [`src/PKSim.Core/Snapshots`](../src/PKSim.Core/Snapshots) define the shape of every
  node. The root object is `Project`.
- The JSON serialization settings in
  [`src/PKSim.Infrastructure/Serialization/Json`](../src/PKSim.Infrastructure/Serialization/Json) determine
  how those classes are written:
  - `NullValueHandling.Ignore` – null-valued properties are omitted, so most properties are optional.
  - `StringEnumConverter` – enums are serialized as their string names.
  - `WritablePropertiesOnlyResolver` – only writable properties are serialized.
  - `ColorConverter` – `Color` values are serialized as `#RRGGBB` / `#AARRGGBB` hex strings.

## Conventions

- Only properties annotated with `[Required]` in the source classes are marked `required` in the schema.
- Because the snapshot format evolves between PK-Sim versions (and older snapshots contain legacy
  fields such as the individual `Molecules` array), objects intentionally allow additional properties so
  that snapshots created by different versions continue to validate.
- Enum-typed fields are modelled as strings (rather than fixed value lists) to avoid rejecting valid
  values introduced by newer PK-Sim versions.

## Validation

The schema has been verified against the published OSP model, DDI, pediatric and qualification snapshots
referenced in the task, covering snapshot versions used by PK-Sim v10 and v11. Any draft-07 compatible
validator can be used, e.g. in Python:

```python
import json
from jsonschema import Draft7Validator

schema = json.load(open("schema/PK-Sim-Snapshot.schema.json"))
snapshot = json.load(open("Alfentanil-Model.json"))
Draft7Validator(schema).validate(snapshot)
```
