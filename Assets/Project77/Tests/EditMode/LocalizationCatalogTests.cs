using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using Project77.Localization;

namespace Project77.Tests
{
    public sealed class LocalizationCatalogTests
    {
        [Test]
        public void EnglishCatalog_ResolvesStablePlayerFacingKeys()
        {
            Assert.That(
                PlayerText.Get(PlayerTextKey.AppTitle),
                Is.EqualTo("Project 77"));
            Assert.That(
                PlayerText.Get(PlayerTextKey.IntroBegin),
                Is.EqualTo("Begin restoration"));
        }

        [Test]
        public void MissingKey_IsVisibleInsteadOfEmpty()
        {
            Assert.That(
                PlayerText.Get("slice.missing.example"),
                Is.EqualTo("[missing:slice.missing.example]"));
        }

        [Test]
        public void Catalog_UsesRequestedCultureForFormatting()
        {
            var catalog = new TextCatalog(
                CultureInfo.GetCultureInfo("fr-FR"),
                new Dictionary<string, string>
                {
                    ["test.number"] = "Value {0:0.0}"
                });

            Assert.That(
                catalog.Format("test.number", 1.5),
                Is.EqualTo("Value 1,5"));
        }

        [Test]
        public void Catalog_CanFallbackWithoutHidingMissingKeys()
        {
            var catalog = new TextCatalog(
                CultureInfo.GetCultureInfo("fr-FR"),
                new Dictionary<string, string>
                {
                    [PlayerTextKey.SliceSubtitle] =
                        "Une tranche verticale beaucoup plus longue pour tester l'expansion du texte."
                },
                PlayerText.English);

            Assert.That(
                catalog.Get(PlayerTextKey.SliceSubtitle),
                Does.Contain("beaucoup plus longue"));
            Assert.That(
                catalog.Get(PlayerTextKey.AppTitle),
                Is.EqualTo("Project 77"));
            Assert.That(
                catalog.Get("slice.unknown"),
                Is.EqualTo("[missing:slice.unknown]"));
        }

        [Test]
        public void InvalidFormat_ReturnsVisibleMarker()
        {
            var catalog = new TextCatalog(
                CultureInfo.InvariantCulture,
                new Dictionary<string, string>
                {
                    ["test.invalid"] = "Broken {0"
                });

            Assert.That(
                catalog.Format("test.invalid", "value"),
                Is.EqualTo("[format-error:test.invalid]"));
        }
    }
}
