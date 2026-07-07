# PK-Sim snapshot JSON schema

`pksim-snapshot-schema.json` is a [JSON Schema](https://json-schema.org/) (draft-07)
describing the structure of a **PK-Sim snapshot** file (a serialized project).

## Source of truth

The schema was derived from the snapshot object model and its serialization settings:

- Snapshot classes: [`src/PKSim.Core/Snapshots`](../src/PKSim.Core/Snapshots)
- JSON serialization: [`src/PKSim.Infrastructure/Serialization/Json`](../src/PKSim.Infrastructure/Serialization/Json)
- Snapshot services: [`src/PKSim.Infrastructure/Services`](../src/PKSim.Infrastructure/Services)

The root object of a snapshot file is `Project`.

## Serialization conventions reflected in the schema

These follow `PKSimJsonSerializerSettings`:

- `NullValueHandling.Ignore` &mdash; `null` properties are omitted, so no property other
  than `Project.Version` is required.
- `StringEnumConverter` &mdash; enum values are written as strings. Every property whose
  underlying type is an enum is restricted to its allowed values in the schema: simple
  enums use an `"enum"` list, while `[Flags]` enums (e.g. `Localization`, `TransportType`,
  `QuantityType`, `PivotArea`, `PKSimBuildingBlockType`) use a `"pattern"` that accepts a
  single value or a comma-separated combination of the valid member names.
- `ColorConverter` &mdash; `System.Drawing.Color` values are written as hex strings
  (`#RRGGBB`, or `#AARRGGBB` when an alpha channel is present).
- `WritablePropertiesOnlyResolver` &mdash; only writable members are serialized.
- The `IEnumerable` snapshot wrappers `CalculationMethodCache`, `OutputSchema` and
  `OutputSelections` are serialized as plain JSON arrays.
- Non-finite floating point values in numeric arrays are written by JSON.NET as the
  strings `"NaN"`, `"Infinity"` and `"-Infinity"` (see `FloatValue`).

## Verification

The schema was validated against the following published PK-Sim snapshots and adjusted
until all validated without errors:

Model snapshots: Alfentanil, Alprazolam, Carbamazepine, Cimetidine, Clarithromycin,
Efavirenz, Erythromycin, Fluvoxamine, Itraconazole, Midazolam, Rifampicin, Triazolam,
Verapamil, Sufentanil.

DDI snapshots: Carbamazepine-Midazolam, Cimetidine-Verapamil, Erythromycin-Alprazolam,
Erythromycin-Carbamazepine, Itraconazole-Alprazolam, Itraconazole-Midazolam,
Verapamil-Midazolam, Rifampicin-Midazolam, Rifampicin-Verapamil.

### Validate a snapshot locally

```bash
pip install jsonschema
python -c "import json,sys; from jsonschema import Draft7Validator; \
Draft7Validator(json.load(open('schemas/pksim-snapshot-schema.json'))).validate(json.load(open(sys.argv[1])))" \
  your-snapshot.json
```
