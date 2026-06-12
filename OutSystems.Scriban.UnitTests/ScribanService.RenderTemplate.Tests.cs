using NUnit.Framework;
using System;

namespace OutSystems.Scriban.UnitTests
{
    /// <summary>
    /// Covers ScribanService.RenderTemplate: native Scriban syntax,
    /// JSON model marshalling (scalars, nested objects, arrays), and error paths.
    /// </summary>
    [TestFixture]
    public class ScribanService_RenderTemplate_Tests
    {
        private readonly ScribanService _sut = new();

        #region Helpers

        private string Render(string template, string modelJson)
        {
            _sut.RenderTemplate(template, modelJson, out var output);
            return output;
        }

        #endregion

        #region Happy path

        [Test]
        public void RenderTemplate_LiteralTextNoModel_ReturnsLiteral()
        {
            var result = Render("Hello, world!", string.Empty);
            Assert.That(result, Is.EqualTo("Hello, world!"));
        }

        [Test]
        public void RenderTemplate_TopLevelStringVariable_RendersValue()
        {
            var result = Render("Hello, {{ name }}!", "{\"name\":\"Ana\"}");
            Assert.That(result, Is.EqualTo("Hello, Ana!"));
        }

        [Test]
        public void RenderTemplate_TopLevelIntegerVariable_RendersValue()
        {
            var result = Render("Count: {{ count }}", "{\"count\":42}");
            Assert.That(result, Is.EqualTo("Count: 42"));
        }

        [Test]
        public void RenderTemplate_BooleanCondition_RendersTrueBranch()
        {
            var template = "{{ if active }}ON{{ else }}OFF{{ end }}";
            var result = Render(template, "{\"active\":true}");
            Assert.That(result, Is.EqualTo("ON"));
        }

        [Test]
        public void RenderTemplate_NestedObjectAccess_RendersNestedValue()
        {
            var template = "{{ user.profile.email }}";
            var json = "{\"user\":{\"profile\":{\"email\":\"a@b.com\"}}}";
            var result = Render(template, json);
            Assert.That(result, Is.EqualTo("a@b.com"));
        }

        [Test]
        public void RenderTemplate_ArrayIteration_RendersEachItem()
        {
            var template = "{{ for item in items }}{{ item }};{{ end }}";
            var json = "{\"items\":[\"a\",\"b\",\"c\"]}";
            var result = Render(template, json);
            Assert.That(result, Is.EqualTo("a;b;c;"));
        }

        [Test]
        public void RenderTemplate_ArrayOfObjects_RendersFieldOfEach()
        {
            var template = "{{ for u in users }}[{{ u.name }}]{{ end }}";
            var json = "{\"users\":[{\"name\":\"x\"},{\"name\":\"y\"}]}";
            var result = Render(template, json);
            Assert.That(result, Is.EqualTo("[x][y]"));
        }

        [Test]
        public void RenderTemplate_CamelCaseKey_PreservedNotSnakeCased()
        {
            // Verifies our member renamer disables Scriban's default snake_case mapping.
            var result = Render("{{ firstName }}", "{\"firstName\":\"Ana\"}");
            Assert.That(result, Is.EqualTo("Ana"));
        }

        [Test]
        public void RenderTemplate_DecimalNumber_RendersAsDouble()
        {
            var result = Render("{{ price }}", "{\"price\":3.14}");
            Assert.That(result, Is.EqualTo("3.14"));
        }

        [Test]
        public void RenderTemplate_NullModelJson_RendersWithEmptyContext()
        {
            _sut.RenderTemplate("Hello", null!, out var output);
            Assert.That(output, Is.EqualTo("Hello"));
        }

        [Test]
        public void RenderTemplate_JsonNullValue_RendersAsEmpty()
        {
            // Scriban renders null/missing variables as empty string.
            var result = Render("[{{ name }}]", "{\"name\":null}");
            Assert.That(result, Is.EqualTo("[]"));
        }

        [Test]
        public void RenderTemplate_EmptyJsonObject_RendersLiteralBody()
        {
            var result = Render("hello", "{}");
            Assert.That(result, Is.EqualTo("hello"));
        }

        [Test]
        public void RenderTemplate_EmptyArray_IteratesZeroTimes()
        {
            var template = "[{{ for item in items }}{{ item }}{{ end }}]";
            var result = Render(template, "{\"items\":[]}");
            Assert.That(result, Is.EqualTo("[]"));
        }

        [Test]
        public void RenderTemplate_NumberLargerThanLong_RendersAsDouble()
        {
            // 1e20 overflows long; converter falls back to double, which Scriban renders in scientific form.
            var result = Render("{{ big }}", "{\"big\":1e20}");
            Assert.That(result, Is.EqualTo("1E+20"));
        }

        [Test]
        public void RenderTemplate_DeeplyNestedModel_RendersLeafValue()
        {
            // 12 levels deep — comfortably under the JsonDocument MaxDepth=64 ceiling.
            var json = "{\"a\":{\"b\":{\"c\":{\"d\":{\"e\":{\"f\":{\"g\":{\"h\":{\"i\":{\"j\":{\"k\":{\"l\":\"deep\"}}}}}}}}}}}}";
            var result = Render("{{ a.b.c.d.e.f.g.h.i.j.k.l }}", json);
            Assert.That(result, Is.EqualTo("deep"));
        }

        #endregion

        #region Resource limits

        [Test]
        public void RenderTemplate_OversizeTemplate_ThrowsArgumentException()
        {
            // 1 MiB + 1 character exceeds the documented input cap.
            var huge = new string('a', 1_048_577);
            var ex = Assert.Throws<ArgumentException>(() =>
                _sut.RenderTemplate(huge, "{}", out _));
            Assert.That(ex!.ParamName, Is.EqualTo("template"));
        }

        [Test]
        public void RenderTemplate_OversizeModelJson_ThrowsArgumentException()
        {
            var oversizeJson = "{\"x\":\"" + new string('a', 1_048_570) + "\"}";
            var ex = Assert.Throws<ArgumentException>(() =>
                _sut.RenderTemplate("{{ x }}", oversizeJson, out _));
            Assert.That(ex!.ParamName, Is.EqualTo("modelJson"));
        }

        [Test]
        public void RenderTemplate_LoopExceedingLimit_ThrowsScribanRuntimeError()
        {
            // Loop limit is 10_000; 20_000 iterations must abort.
            var template = "{{ for i in 1..20000 }}.{{ end }}";
            Assert.Throws<global::Scriban.Syntax.ScriptRuntimeException>(() =>
                _sut.RenderTemplate(template, "{}", out _));
        }

        #endregion

        #region Error paths

        [Test]
        public void RenderTemplate_NullTemplate_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _sut.RenderTemplate(null!, "{}", out _));
        }

        [Test]
        public void RenderTemplate_InvalidTemplate_ThrowsWithDescriptiveMessage()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                _sut.RenderTemplate("{{ if }}", "{}", out _));
            Assert.That(ex!.Message, Does.Contain("Scriban template parse failed"));
        }

        [Test]
        public void RenderTemplate_MalformedJson_ThrowsJsonException()
        {
            // Assert.Catch matches the requested exception type OR any subclass
            // (System.Text.Json throws JsonReaderException, which derives from JsonException).
            Assert.Catch<System.Text.Json.JsonException>(() =>
                _sut.RenderTemplate("{{ x }}", "{not-json", out _));
        }

        [Test]
        public void RenderTemplate_JsonRootIsArray_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                _sut.RenderTemplate("{{ x }}", "[1,2,3]", out _));
            Assert.That(ex!.Message, Does.Contain("JSON object at the root"));
        }

        #endregion
    }
}
