using NUnit.Framework;
using System;

namespace OutSystems.Scriban.UnitTests
{
    /// <summary>
    /// Covers ScribanService.ValidateTemplate: success, failure, and edge cases.
    /// </summary>
    [TestFixture]
    public class ScribanService_ValidateTemplate_Tests
    {
        private readonly ScribanService _sut = new();

        #region Helpers

        private ValidationResult Validate(string template)
        {
            _sut.ValidateTemplate(template, out var result);
            return result;
        }

        #endregion

        #region Valid templates

        [Test]
        public void ValidateTemplate_ValidScribanTemplate_ReturnsIsValidTrue()
        {
            var result = Validate("Hello {{ name }}!");

            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(result.ErrorMessage, Is.EqualTo(string.Empty));
            });
        }

        [Test]
        public void ValidateTemplate_EmptyTemplate_ReturnsIsValidTrue()
        {
            var result = Validate(string.Empty);
            Assert.That(result.IsValid, Is.True);
        }

        #endregion

        #region Invalid templates

        [Test]
        public void ValidateTemplate_InvalidScribanTemplate_ReturnsIsValidFalseWithMessage()
        {
            var result = Validate("{{ if }}");

            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.ErrorMessage, Is.Not.Empty);
            });
        }

        [Test]
        public void ValidateTemplate_InvalidTemplate_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                _sut.ValidateTemplate("{{ if }}", out _));
        }

        #endregion

        #region Error paths

        [Test]
        public void ValidateTemplate_NullTemplate_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _sut.ValidateTemplate(null!, out _));
        }

        #endregion
    }
}
