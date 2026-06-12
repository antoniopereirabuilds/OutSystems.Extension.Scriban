using OutSystems.ExternalLibraries.SDK;

namespace OutSystems.Scriban
{
    /// <summary>
    /// Renders text templates using the Scriban templating engine.
    /// Model data is supplied as a JSON string so any OutSystems data shape
    /// can be passed without requiring custom marshalling structures.
    /// </summary>
    /// <remarks>
    /// Runs as an AWS Lambda function (linux-x64, stateless). Each call is independent — do not rely on in-memory state between invocations.
    /// Exposed to ODC Studio via <c>OutSystems.ExternalLibraries.SDK</c> attributes.
    /// </remarks>
    [OSInterface(
        Description = "Renders text templates using the Scriban templating engine.",
        IconResourceName = "OutSystems.Scriban.resources.Scriban_icon.png",
        Name = "ScribanService"
    )]
    public interface IScribanService
    {
        /// <summary>
        /// Parses and renders a Scriban template against a JSON model.
        /// </summary>
        /// <param name="template">The Scriban template text. Must not be null. Maximum length 1,048,576 characters (1 MiB).</param>
        /// <param name="modelJson">JSON object whose properties are exposed as template variables. May be null or empty for an empty model. Maximum length 1,048,576 characters (1 MiB). Root must be a JSON object — arrays are rejected. JSON keys map verbatim to template variable names (no snake_case renaming).</param>
        /// <param name="renderedText">The rendered output text.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="template"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="template"/> or <paramref name="modelJson"/> exceeds 1 MiB, or when <paramref name="modelJson"/> is non-empty but its root is not a JSON object.</exception>
        /// <exception cref="System.Text.Json.JsonException">Thrown when <paramref name="modelJson"/> is not valid JSON or exceeds the depth limit of 64 levels.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the template fails to parse; the message aggregates all parser diagnostics.</exception>
        /// <exception cref="Scriban.Syntax.ScriptRuntimeException">Thrown at render time when the template exceeds the configured loop limit (10,000), object recursion limit (100), or regex timeout (1 second).</exception>
        /// <remarks>
        /// ODC mapping: Server Action <c>RenderTemplate</c> in the <c>ScribanService</c> external library.
        /// </remarks>
        [OSAction(
            Description = "Parses and renders a Scriban template against a JSON model. Throws on parse or render errors.",
            IconResourceName = "OutSystems.Scriban.resources.Scriban_icon.png"
        )]
        void RenderTemplate(
            [OSParameterAttribute(Description = "The Scriban template text to render.")]
            string template,

            [OSParameterAttribute(Description = "JSON object whose properties are exposed as template variables. May be empty for no model.")]
            string modelJson,

            [OSParameterAttribute(Description = "The rendered output text.")]
            out string renderedText
        );

        /// <summary>
        /// Parses a template without rendering it and reports whether it is syntactically valid.
        /// Parse-only — no execution side effects occur even for templates that would loop forever or reference nonexistent files.
        /// </summary>
        /// <param name="template">The template text to validate. Must not be null. Maximum length 1,048,576 characters (1 MiB).</param>
        /// <param name="result">Validation outcome including the first parse error message when invalid.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="template"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="template"/> exceeds 1 MiB.</exception>
        /// <remarks>
        /// ODC mapping: Server Action <c>ValidateTemplate</c> in the <c>ScribanService</c> external library.
        /// Syntactic failures are reported on <see cref="ValidationResult"/>; only the size and null guards raise exceptions.
        /// </remarks>
        [OSAction(
            Description = "Parses a Scriban template without rendering it and reports whether it is syntactically valid. Does not throw on invalid templates.",
            IconResourceName = "OutSystems.Scriban.resources.Scriban_icon.png"
        )]
        void ValidateTemplate(
            [OSParameterAttribute(Description = "The template text to validate.")]
            string template,

            [OSParameterAttribute(Description = "Validation outcome including the first parse error message when invalid.")]
            out ValidationResult result
        );
    }
}
