OutSystems.Scriban lets ODC applications render text templates against JSON model data using the Scriban templating engine, so any OutSystems record or list can drive a template without bespoke marshalling structures.

Key capabilities. Render template parses a Scriban template and produces the final text in a single action. Validate template parses a template without executing it and returns whether it is syntactically valid, with the first error message when invalid. JSON keys map verbatim to template variables with full support for nested objects, arrays, strings, numbers and booleans.

The model is supplied as a JSON object string; arrays at the root are rejected. Template and model are each limited to one MiB, JSON nesting to 64 levels, and rendering is bounded to 10,000 loop iterations and a one second regex timeout to stop runaway templates.

Ideal for ODC apps that generate emails, documents, or configuration files from structured data without calling an external service.
