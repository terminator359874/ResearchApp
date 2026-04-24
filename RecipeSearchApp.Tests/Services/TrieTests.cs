using Xunit;
using FluentAssertions;
using RecipeSearchApp.Services;

namespace RecipeSearchApp.Tests.Services
{
    public class TrieTests
    {
        [Fact]
        public void Insert_SingleWord_ShouldBeSearchable()
        {
            var trie = new Trie();
            var word = "Борщ";

            trie.Insert(word);
            var results = trie.SearchByPrefix("Бор");

            results.Should().Contain(word);
        }

        [Fact]
        public void SearchByPrefix_EmptyPrefix_ShouldReturnEmpty()
        {
            var trie = new Trie();
            trie.Insert("Борщ");

            var results = trie.SearchByPrefix("");

            results.Should().BeEmpty();
        }

        [Fact]
        public void SearchByPrefix_NonExistentPrefix_ShouldReturnEmpty()
        {
            var trie = new Trie();
            trie.Insert("Борщ");
            trie.Insert("Блины");

            var results = trie.SearchByPrefix("Сал");

            results.Should().BeEmpty();
        }

        [Fact]
        public void SearchByPrefix_CaseInsensitive_ShouldFindWord()
        {
            var trie = new Trie();
            trie.Insert("Борщ");

            var results = trie.SearchByPrefix("бор");

            results.Should().Contain("Борщ");
        }

        [Theory]
        [InlineData("Б", 2)]
        [InlineData("Бл", 1)]
        [InlineData("Бо", 1)]
        public void SearchByPrefix_MultipleWords_ShouldReturnCorrectCount(string prefix, int expectedCount)
        {
            var trie = new Trie();
            trie.Insert("Борщ");
            trie.Insert("Блины");

            var results = trie.SearchByPrefix(prefix);

            results.Should().HaveCount(expectedCount);
        }

        [Fact]
        public void SearchByPrefix_LimitResults_ShouldRespectMaxResults()
        {
            var trie = new Trie();
            for (int i = 0; i < 20; i++)
                trie.Insert($"Блюдо{i}");

            var results = trie.SearchByPrefix("Блю", maxResults: 5);

            results.Should().HaveCountLessOrEqualTo(5);
        }

        [Fact]
        public void Clear_AfterInserts_ShouldRemoveAllWords()
        {
            var trie = new Trie();
            trie.Insert("Борщ");
            trie.Insert("Блины");

            trie.Clear();
            var results = trie.SearchByPrefix("Б");

            results.Should().BeEmpty();
        }
    }
}
