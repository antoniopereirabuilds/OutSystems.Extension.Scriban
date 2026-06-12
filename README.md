![Tests](https://github.com/antoniobgpereira/OutSystems.Extension.Scriban/actions/workflows/test.yml/badge.svg) ![Release](https://github.com/antoniobgpereira/OutSystems.Extension.Scriban/actions/workflows/release.yml/badge.svg)

# OutSystems.Extension.Scriban — OutSystems ODC External Library

> Renders Scriban text templates inside ODC apps using a JSON model, with no bespoke marshalling structures.

## Overview

ODC External Library that wraps the [Scriban](https://github.com/scriban/scriban) templating engine (v7.2.4) for OutSystems Developer Cloud. Templates are rendered against model data supplied as a JSON string, so any OutSystems record, list, or structure can drive a template by serialising to JSON first. Two server actions are exposed: one to render and one to validate.

## Structures

| Structure | Description | Fields |
|-----------|-------------|--------|
| ValidationResult | Outcome of a template validation. `ErrorMessage` is empty when `IsValid` is true. | `IsValid` (Boolean), `ErrorMessage` (Text) |

### ValidationResult fields

| Field | Type | Description |
|-------|------|-------------|
| IsValid | Boolean | True when the template parsed successfully. |
| ErrorMessage | Text | First parse error message; empty when `IsValid` is true. |

## Actions

| Action | Description | Key Inputs | Key Outputs |
|--------|-------------|-----------|-------------|
| RenderTemplate | Parses and renders a Scriban template against a JSON model. Throws on parse or render errors. | `template` (Text), `modelJson` (Text) | `renderedText` (Text) |
| ValidateTemplate | Parses a template without rendering and reports whether it is syntactically valid. Never throws for syntactic errors. | `template` (Text) | `result` (ValidationResult) |

### RenderTemplate

Parses and renders a Scriban template against a JSON model.

| Direction | Parameter | Type | Description |
|-----------|-----------|------|-------------|
| Input | template | Text | The Scriban template text to render. Maximum 1 MiB. |
| Input | modelJson | Text | JSON object whose properties are exposed as template variables. Maximum 1 MiB. May be empty for no model. |
| Output | renderedText | Text | The rendered output text. |

**Exceptions thrown**

| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | `template` is null. |
| `ArgumentException` | `template` or `modelJson` exceeds 1 MiB, or `modelJson` root is not a JSON object. |
| `System.Text.Json.JsonException` | `modelJson` is not valid JSON or exceeds 64 levels of nesting. |
| `InvalidOperationException` | Template fails to parse. Message aggregates all parser diagnostics. |
| `Scriban.Syntax.ScriptRuntimeException` | Render exceeds loop limit (10,000), object recursion limit (100), or regex timeout (1 s). |

### ValidateTemplate

Parses a template without rendering and reports whether it is syntactically valid. Parse-only — no execution side effects occur even for templates that would loop forever or reference nonexistent files.

| Direction | Parameter | Type | Description |
|-----------|-----------|------|-------------|
| Input | template | Text | The template text to validate. Maximum 1 MiB. |
| Output | result | ValidationResult | Validation outcome including the first parse error message when invalid. |

**Exceptions thrown**

| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | `template` is null. |
| `ArgumentException` | `template` exceeds 1 MiB. |

Syntactic errors are reported on `result.IsValid` / `result.ErrorMessage` — they do not throw.

## Usage Notes

- **JSON model shape:** `modelJson` must be a JSON object at the root (e.g. `{"user":{"name":"Ana"}}`). A JSON array at the root is rejected with an `ArgumentException`. Pass an empty string when no model is needed.
- **Variable naming:** JSON keys are exposed verbatim as template variable names (no snake_case rewriting). `{"firstName":"Ana"}` is referenced as `{{ firstName }}`, not `{{ first_name }}`.
- **Type mapping:** JSON strings, booleans, integer numbers (mapped to `long`), decimal numbers (mapped to `double`), arrays, and nested objects are all supported. JSON `null` values render as empty string. Numbers that overflow `long` fall back to `double` and render in scientific notation for very large values.
- **Error surface:** `RenderTemplate` throws `InvalidOperationException` with the aggregated Scriban parser messages on parse failure. Use `ValidateTemplate` to pre-flight a template without throwing.
- **Resource limits:**
  - `template` and `modelJson` are each capped at **1 MiB (1,048,576 characters)**. Oversize inputs throw `ArgumentException`.
  - JSON nesting is capped at **64 levels**. Deeper payloads throw `System.Text.Json.JsonException`.
  - Templates are bounded at **10,000 loop iterations**, **100 levels of object recursion**, and **1-second regex evaluation**. Exceeding any runtime bound throws `Scriban.Syntax.ScriptRuntimeException`.

### Example

Template:

```
Hi {{ user.name }}, you have {{ items.size }} items:
{{ for item in items }}- {{ item }}
{{ end }}
```

Model JSON:

```json
{
  "user": { "name": "Ana" },
  "items": ["apples", "pears", "oranges"]
}
```

Output:

```
Hi Ana, you have 3 items:
- apples
- pears
- oranges
```

## Technical Details

- **Library:** [Scriban](https://www.nuget.org/packages/Scriban) v7.2.4
- **Target:** .NET 8.0, linux-x64 (framework-dependent)
- **ODC SDK:** `OutSystems.ExternalLibraries.SDK` v1.5.0
- **License:** BSD-3-Clause

## Building Locally

```bash
dotnet restore OutSystems.Scriban.sln
dotnet build OutSystems.Scriban.sln -c Release
dotnet test OutSystems.Scriban.UnitTests/OutSystems.Scriban.UnitTests.csproj -c Release
```

## Packaging for ODC

```bash
dotnet publish OutSystems.Scriban/OutSystems.Scriban.csproj \
  -c Release -r linux-x64 --self-contained false -o ./publish
cd ./publish && zip -r ../Scriban_Asset.zip . && cd ..
```

Upload `Scriban_Asset.zip` to ODC Portal under **External Libraries**.
