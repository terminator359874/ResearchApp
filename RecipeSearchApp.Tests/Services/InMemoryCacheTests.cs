using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using RecipeSearchApp.Models;
using RecipeSearchApp.Services;

namespace RecipeSearchApp.Tests.Services
{
    public class InMemoryCacheTests
    {
        [Fact]
        public void Set_And_Get_ShouldReturnSameRecipe()
        {
            var cache = new InMemoryCache();
            var recipe = new Recipe
            {
                Id = 1,
                Title = "Тестовый рецепт",
                Ingredients = "тест",
                Instructions = "тест",
                Category = "Тесты",
                PrepTimeMinutes = 10
            };

            cache.Set(1, recipe);
            var retrieved = cache.Get(1);

            retrieved.Should().NotBeNull();
            retrieved!.Title.Should().Be("Тестовый рецепт");
        }

        [Fact]
        public void Get_NonExistentKey_ShouldReturnNull()
        {
            var cache = new InMemoryCache();

            var result = cache.Get(999);

            result.Should().BeNull();
        }

        [Fact]
        public void Set_ExceedingMaxSize_ShouldEvictOldest()
        {
            var cache = new InMemoryCache(maxSize: 3);
            for (int i = 1; i <= 4; i++)
            {
                cache.Set(i, new Recipe { Id = i, Title = $"Recipe {i}" });
            }

            var oldest = cache.Get(1);
            var newest = cache.Get(4);

            oldest.Should().BeNull();
            newest.Should().NotBeNull();
        }

        [Fact]
        public void Get_UpdatesLastAccessTime()
        {
            var cache = new InMemoryCache(maxSize: 2);
            cache.Set(1, new Recipe { Id = 1, Title = "First" });
            cache.Set(2, new Recipe { Id = 2, Title = "Second" });

            cache.Get(1);
            cache.Set(3, new Recipe { Id = 3, Title = "Third" });

            cache.Get(1).Should().NotBeNull();
            cache.Get(2).Should().BeNull();
        }

        [Fact]
        public void Clear_ShouldRemoveAllEntries()
        {
            var cache = new InMemoryCache();
            cache.Set(1, new Recipe { Id = 1, Title = "Test" });
            cache.Set(2, new Recipe { Id = 2, Title = "Test2" });

            cache.Clear();

            cache.Get(1).Should().BeNull();
            cache.Get(2).Should().BeNull();
        }

        [Fact]
        public void GetStatistics_ShouldReturnCorrectData()
        {
            var cache = new InMemoryCache();
            cache.Set(1, new Recipe { Id = 1, Title = "Test" });

            cache.Get(1);
            cache.Get(1);
            var stats = cache.GetStatistics();

            stats["CachedItems"].Should().Be(1);
            stats["TotalHits"].Should().Be(2);
        }

        [Fact]
        public async Task Get_ExpiredEntry_ShouldReturnNull()
        {
            var cache = new InMemoryCache(expirationMinutes: 0);
            cache.Set(1, new Recipe { Id = 1, Title = "Test" });

            await Task.Delay(100);
            var result = cache.Get(1);

            result.Should().BeNull();
        }
    }
}
