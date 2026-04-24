using Xunit;
using FluentAssertions;
using RecipeSearchApp.Services;

namespace RecipeSearchApp.Tests.Services
{
    public class StringSearcherTests
    {
        [Theory]
        [InlineData("Hello World", "World", 1)]
        [InlineData("test test test", "test", 3)]
        [InlineData("abcdef", "xyz", 0)]
        public void KnuthMorrisPratt_ShouldFindCorrectMatches(string text, string pattern, int expectedCount)
        {
            var positions = StringSearcher.KnuthMorrisPratt(text, pattern);
            positions.Should().HaveCount(expectedCount);
        }

        [Fact]
        public void KnuthMorrisPratt_ShouldFindAtCorrectPosition()
        {
            var text = "Отварить картофель и морковь";
            var pattern = "картофель";
            var positions = StringSearcher.KnuthMorrisPratt(text, pattern);

            positions.Should().ContainSingle();
            positions[0].Should().Be(9);
        }

        [Theory]
        [InlineData("Hello World", "World", 1)]
        [InlineData("test test test", "test", 3)]
        [InlineData("abcdef", "xyz", 0)]
        public void BoyerMoore_ShouldFindCorrectMatches(string text, string pattern, int expectedCount)
        {
            var positions = StringSearcher.BoyerMoore(text, pattern);
            positions.Should().HaveCount(expectedCount);
        }

        [Fact]
        public void KMP_And_BM_ShouldReturnSameResults()
        {
            var text = "Нарезать морковь, лук и морковь кубиками";
            var pattern = "морковь";

            var kmpResults = StringSearcher.KnuthMorrisPratt(text, pattern);
            var bmResults = StringSearcher.BoyerMoore(text, pattern);

            kmpResults.Should().BeEquivalentTo(bmResults);
        }

        [Fact]
        public void HighlightMatches_ShouldInsertMarkers()
        {
            var text = "Добавить соль и перец";
            var pattern = "соль";

            var highlighted = StringSearcher.HighlightMatches(text, pattern, "KMP");

            highlighted.Should().Contain("【соль】");
        }

        [Fact]
        public void HighlightMatches_MultipleOccurrences_ShouldHighlightAll()
        {
            var text = "морковь и морковь";
            var pattern = "морковь";

            var highlighted = StringSearcher.HighlightMatches(text, pattern);

            highlighted.Should().Contain("【морковь】 и 【морковь】");
        }

        [Fact]
        public void HighlightMatches_EmptyPattern_ShouldReturnOriginal()
        {
            var text = "Test text";
            var result = StringSearcher.HighlightMatches(text, "");

            result.Should().Be(text);
        }
    }
}
