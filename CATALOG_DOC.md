## What it does

OutSystems.Scriban renders text templates inside ODC applications using the Scriban templating engine. Model data is passed in as a JSON object string, which lets any OutSystems record, list, or structure drive a template after a single serialisation step. The library also offers a parse-only validation action so a template can be checked for syntactic correctness before it is ever executed.

## Actions

| Action | What it does | Key inputs | Key outputs |
|--------|-------------|-----------|-------------|
| RenderTemplate | Parses and renders a Scriban template against a JSON model. Throws on parse, JSON, or render errors. | template: Text, modelJson: Text | renderedText: Text |
| ValidateTemplate | Parses a template without rendering it and reports whether it is syntactically valid. Never throws for syntactic errors. | template: Text | result: ValidationResult |

## Structures

| Structure | What it represents | Key fields |
|-----------|--------------------|-----------|
| ValidationResult | Outcome of a template validation. ErrorMessage is empty when IsValid is true. | IsValid: Boolean, ErrorMessage: Text |

## How to use

1. Build your Scriban template as a Text expression in ODC Studio. Reference variables with the usual Scriban syntax, for example a greeting that reads user.name from the model.
2. Serialise your data to a JSON object using the ODC JSON Serialize action. The JSON root must be an object; arrays at the root are rejected.
3. Drag the RenderTemplate server action into your flow. Map the template Text to the template input and the JSON string to the modelJson input. Read the rendered output from renderedText.
4. Optionally call ValidateTemplate first when the template comes from user input or configuration data. Check the IsValid flag and surface ErrorMessage in the UI when the template is malformed.
5. Catch exceptions from RenderTemplate if the template might fail at runtime. The library throws ArgumentException for oversize or malformed inputs, JsonException for invalid JSON, InvalidOperationException for parse failures, and a Scriban runtime exception when execution bounds are exceeded.

JSON keys are exposed verbatim as template variable names, so a JSON property called firstName is referenced as firstName in the template and not as first_name. JSON null values render as empty string, and very large numbers that exceed the long range fall back to double, which may appear in scientific notation.

## Constraints

- Each call is independent. The library is stateless, so do not rely on data persisting between renders.
- The template input is limited to one MiB and oversize inputs are rejected.
- The modelJson input is limited to one MiB. It must be a JSON object at the root; arrays are rejected. Nested objects beyond 64 levels deep are rejected.
- Rendering is bounded to 10,000 loop iterations, 100 levels of object recursion, and a one second regex evaluation timeout. Templates that exceed any of these bounds fail with a Scriban runtime exception, which keeps a single bad template from monopolising the worker.
- ValidateTemplate is parse only. It never executes the template, so templates that would loop forever or reference nonexistent files still validate cleanly as long as they parse.
