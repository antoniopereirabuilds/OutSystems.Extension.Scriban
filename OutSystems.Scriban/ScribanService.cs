using System;
using System.Collections.Generic;
using System.Text.Json;
using Scriban;
using Scriban.Runtime;

namespace OutSystems.Scriban
{
    /// <summary>
    /// Default implementation of <see cref="IScribanService"/>.
    /// </summary>
    /// <remarks>
    /// Stateless — safe to instantiate per call. The class enforces input size, JSON depth, and runtime bounds
    /// to keep a single invocation from monopolising the Lambda worker.
    /// </remarks>
    public class ScribanService : IScribanService
    {
        /// <summary>Hard cap on <c>template</c> and <c>modelJson</c> input size (1 MiB) to bound parser and memory cost.</summary>
        private const int MaxInputLength = 1_048_576;

        /// <summary>Maximum number of loop iterations a single render may perform before Scriban aborts with a runtime exception.</summary>
        private const int LoopLimit = 10_000;

        /// <summary>Maximum object recursion depth a single render may reach before Scriban aborts with a runtime exception.</summary>
        private const int ObjectRecursionLimit = 100;

        /// <summary>Maximum time a single regex evaluation may take before Scriban aborts with a runtime exception.</summary>
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

        /// <summary>JSON parser options applied to <c>modelJson</c>. Caps nesting at 64 levels to reject deeply nested payloads.</summary>
        private static readonly JsonDocumentOptions JsonOptions = new() { MaxDepth = 64 };

        /// <inheritdoc />
        public void RenderTemplate(string template, string modelJson, out string renderedText)
        {
            ArgumentNullException.ThrowIfNull(template);
            EnsureWithinSizeLimit(template, nameof(template));
            EnsureWithinSizeLimit(modelJson, nameof(modelJson));
            renderedText = string.Empty;

            var parsed = Template.Parse(template);
            ThrowIfHasErrors(parsed, "Scriban template parse failed");

            var context = BuildTemplateContext(modelJson);
            renderedText = parsed.Render(context);
        }

        /// <inheritdoc />
        public void ValidateTemplate(string template, out ValidationResult result)
        {
            ArgumentNullException.ThrowIfNull(template);
            EnsureWithinSizeLimit(template, nameof(template));

            result = new ValidationResult { IsValid = false, ErrorMessage = string.Empty };

            var parsed = Template.Parse(template);

            if (parsed.HasErrors)
            {
                var firstError = parsed.Messages.Count > 0
                    ? parsed.Messages[0].ToString()
                    : "Unknown parse error.";

                result = new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = firstError
                };
                return;
            }

            result = new ValidationResult
            {
                IsValid = true,
                ErrorMessage = string.Empty
            };
        }

        /// <summary>
        /// Builds a Scriban <see cref="TemplateContext"/> from a JSON model string.
        /// JSON keys map 1:1 to template variable names (no renaming).
        /// </summary>
        private static TemplateContext BuildTemplateContext(string? modelJson)
        {
            var scriptObject = new ScriptObject();

            if (!string.IsNullOrWhiteSpace(modelJson))
            {
                using var doc = JsonDocument.Parse(modelJson, JsonOptions);

                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    throw new ArgumentException(
                        "modelJson must be a JSON object at the root.",
                        nameof(modelJson));
                }

                foreach (var property in doc.RootElement.EnumerateObject())
                {
                    scriptObject[property.Name] = ConvertJsonElement(property.Value);
                }
            }

            var context = new TemplateContext
            {
                // Map JSON keys verbatim to template variable names — no snake_case renaming.
                MemberRenamer = member => member.Name,
                // Bound execution to prevent runaway templates monopolising the worker.
                LoopLimit = LoopLimit,
                LoopLimitQueryable = LoopLimit,
                ObjectRecursionLimit = ObjectRecursionLimit,
                RegexTimeOut = RegexTimeout
            };
            context.PushGlobal(scriptObject);
            return context;
        }

        /// <summary>
        /// Rejects strings larger than <see cref="MaxInputLength"/> with a clear <see cref="ArgumentException"/>.
        /// Null values are accepted (no-op) so callers can decide separately whether null is permitted.
        /// </summary>
        /// <param name="value">The string to measure. Null is allowed and ignored.</param>
        /// <param name="paramName">Caller parameter name used in the exception message.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> exceeds <see cref="MaxInputLength"/> characters.</exception>
        private static void EnsureWithinSizeLimit(string? value, string paramName)
        {
            if (value is not null && value.Length > MaxInputLength)
            {
                throw new ArgumentException(
                    $"{paramName} exceeds maximum length of {MaxInputLength} characters.",
                    paramName);
            }
        }

        /// <summary>
        /// Recursively converts a <see cref="JsonElement"/> into plain .NET values
        /// that Scriban can navigate (Dictionary, List, primitives).
        /// </summary>
        private static object? ConvertJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var obj = new ScriptObject();
                    foreach (var property in element.EnumerateObject())
                    {
                        obj[property.Name] = ConvertJsonElement(property.Value);
                    }
                    return obj;

                case JsonValueKind.Array:
                    var list = new ScriptArray();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(ConvertJsonElement(item));
                    }
                    return list;

                case JsonValueKind.String:
                    return element.GetString();

                case JsonValueKind.Number:
                    if (element.TryGetInt64(out var longValue))
                    {
                        return longValue;
                    }
                    return element.GetDouble();

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                default:
                    return null;
            }
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> aggregating all parser messages
        /// when the parsed template has errors.
        /// </summary>
        private static void ThrowIfHasErrors(Template parsed, string contextMessage)
        {
            if (!parsed.HasErrors)
            {
                return;
            }

            var messages = new List<string>(parsed.Messages.Count);
            foreach (var message in parsed.Messages)
            {
                messages.Add(message.ToString());
            }

            throw new InvalidOperationException(
                $"{contextMessage}: {string.Join("; ", messages)}");
        }
    }
}
