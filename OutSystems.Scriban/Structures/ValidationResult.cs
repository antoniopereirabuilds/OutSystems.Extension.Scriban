using OutSystems.ExternalLibraries.SDK;

namespace OutSystems.Scriban
{
    /// <summary>
    /// Outcome of a template validation produced by <see cref="IScribanService.ValidateTemplate"/>.
    /// </summary>
    /// <remarks>
    /// Maps to the ODC structure <c>ValidationResult</c>. <see cref="ErrorMessage"/> is empty when <see cref="IsValid"/> is true.
    /// </remarks>
    [OSStructure(Description = "Outcome of a template validation. ErrorMessage is empty when IsValid is true.")]
    public struct ValidationResult
    {
        /// <summary>True when the template parsed successfully; false when one or more parse errors were detected.</summary>
        /// <remarks>Maps to ODC type <c>Boolean</c>.</remarks>
        [OSStructureField(Description = "True when the template parsed successfully.")]
        public bool IsValid;

        /// <summary>First parse error message reported by the Scriban parser; empty string when <see cref="IsValid"/> is true.</summary>
        /// <remarks>Maps to ODC type <c>Text</c>. Only the first diagnostic is surfaced; additional messages are discarded.</remarks>
        [OSStructureField(Description = "First parse error message; empty when IsValid is true.")]
        public string ErrorMessage;
    }
}
