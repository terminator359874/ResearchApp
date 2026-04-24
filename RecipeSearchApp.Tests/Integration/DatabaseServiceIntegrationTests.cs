using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using RecipeSearchApp.Models;
using RecipeSearchApp.Services;

namespace RecipeSearchApp.Tests.Integration
{
    public class DatabaseServiceIntegrationTests : IDisposable
    {
        private readonly string _testDbPath;
        private readonly DatabaseService _database;

        public DatabaseServiceIntegrationTests()
        {
            _testDbPath = Path.Combine(
                Path.GetTempPath(),
                $"test_recipes_{Guid.NewGuid()}.db"
            );

            _database = new DatabaseService(_testDbPath);
            _database.InitializeAsync().Wait();
        }

        [Fact]
        public async Task AddRecipeAsync_ShouldPersistToDatabase()
        {
            var recipe = new Recipe
            {
                Title = "Тестовый рецепт",
                Ingredients = "ингредиент1, ингредиент2",
                Instructions = "Шаг 1. Шаг 2.",
                Category = "Тесты",
                PrepTimeMinutes = 15
            };

            await _database.AddRecipeAsync(recipe);
            var allRecipes = await _database.GetAllRecipesAsync();

            allRecipes.Should().Contain(r => r.Title == "Тестовый рецепт");
        }

        [Fact]
        public async Task FullTextSearchAsync_ShouldFindByTitle()
        {
            var recipe = new Recipe
            {
                Title = "Уникальный борщ",
                Ingredients = "свёкла",
                Instructions = "варить",
                Category = "Супы",
                PrepTimeMinutes = 60
            };
            await _database.AddRecipeAsync(recipe);

            var results = await _database.FullTextSearchAsync("Уникальный");

            results.Should().Contain(r => r.Title.Contains("Уникальный"));
        }

        [Fact]
        public async Task FullTextSearchAsync_ShouldFindByIngredients()
        {
            var recipe = new Recipe
            {
                Title = "Салат",
                Ingredients = "редкий_ингредиент_xyz",
                Instructions = "смешать",
                Category = "Салаты",
                PrepTimeMinutes = 10
            };
            await _database.AddRecipeAsync(recipe);

            var results = await _database.FullTextSearchAsync("редкий_ингредиент_xyz");

            results.Should().Contain(r => r.Ingredients.Contains("редкий_ингредиент_xyz"));
        }

        [Fact]
        public async Task GetRecipeByIdAsync_ShouldReturnCorrectRecipe()
        {
            var allRecipes = await _database.GetAllRecipesAsync();
            if (allRecipes.Count == 0)
            {
                await _database.AddRecipeAsync(new Recipe
                {
                    Title = "Test",
                    Ingredients = "test",
                    Instructions = "test",
                    Category = "Test",
                    PrepTimeMinutes = 1
                });
                allRecipes = await _database.GetAllRecipesAsync();
            }
            var existingId = allRecipes[0].Id;

            var recipe = await _database.GetRecipeByIdAsync(existingId);

            recipe.Should().NotBeNull();
            recipe!.Id.Should().Be(existingId);
        }

        [Fact]
        public async Task GetRecipeByIdAsync_IncreasesViewCount()
        {
            var allRecipes = await _database.GetAllRecipesAsync();
            var recipe = allRecipes.FirstOrDefault();
            if (recipe == null)
            {
                await _database.AddRecipeAsync(new Recipe { Title = "Test", Ingredients = "test", Instructions = "test", Category = "Test", PrepTimeMinutes = 1 });
                allRecipes = await _database.GetAllRecipesAsync();
                recipe = allRecipes[0];
            }
            var initialViewCount = recipe.ViewCount;

            await _database.GetRecipeByIdAsync(recipe.Id);
            var updated = await _database.GetRecipeByIdAsync(recipe.Id);

            updated!.ViewCount.Should().BeGreaterThan(initialViewCount);
        }

        [Fact]
        public void GetTitleSuggestions_ShouldReturnMatches()
        {
            var suggestions = _database.GetTitleSuggestions("Бор");

            suggestions.Should().Contain(s => s.StartsWith("Бор", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void MightHaveIngredient_ExistingIngredient_ShouldReturnTrue()
        {
            var result = _database.MightHaveIngredient("картофель");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task AddRating_ShouldUpdateAverageRating()
        {
            var recipe = new Recipe
            {
                Title = "Тест Рейтинга",
                Ingredients = "тест",
                Instructions = "тест",
                Category = "Тест",
                PrepTimeMinutes = 10
            };
            await _database.AddRecipeAsync(recipe);
            var added = (await _database.GetAllRecipesAsync()).First(r => r.Title == "Тест Рейтинга");

            await _database.AddRatingAsync(added.Id, 5);
            await _database.AddRatingAsync(added.Id, 4);

            var updated = await _database.GetRecipeByIdAsync(added.Id);

            updated!.AverageRating.Should().Be(4.5);
            updated.RatingCount.Should().Be(2);
        }

        public void Dispose()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (File.Exists(_testDbPath))
            {
                try { File.Delete(_testDbPath); } catch { }
            }
        }
    }
}
