using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RecipeSearchApp.Models;

namespace RecipeSearchApp.Services
{
    public interface IDatabaseService
    {
        Task<List<Recipe>> FullTextSearchAsync(string query);
        List<string> GetTitleSuggestions(string prefix);
        bool MightHaveIngredient(string ingredient);
        Task<Recipe?> GetRecipeByIdAsync(int id);
        Task<List<Recipe>> GetAllRecipesAsync();
        Task AddRecipeAsync(Recipe recipe);
        Dictionary<string, object> GetCacheStatistics();
        Task AddRatingAsync(int recipeId, int rating);
        Task InitializeAsync();
    }
}
