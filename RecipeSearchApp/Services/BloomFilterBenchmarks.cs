using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeSearchApp.Services
{
    public class BloomFilterBenchmarkResult
    {
        public int FilterSize { get; set; }
        public int HashCount { get; set; }
        public int FalsePositives { get; set; }
        public double FalsePositiveRate { get; set; }
    }

    public class BloomFilterBenchmarks
    {
        public static async Task<List<BloomFilterBenchmarkResult>> AnalyzeFalsePositivesAsync(DatabaseService db)
        {
            var results = new List<BloomFilterBenchmarkResult>();

            // 1. Get all real ingredients
            var recipes = await db.GetAllRecipesAsync();
            var realIngredients = new HashSet<string>();
            foreach (var recipe in recipes)
            {
                var parts = recipe.Ingredients.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    realIngredients.Add(part.ToLowerInvariant());
                }
            }

            // 2. Generate 1000 missing ingredients
            var random = new Random(42);
            var missingIngredients = new List<string>();
            while (missingIngredients.Count < 1000)
            {
                var fakeWord = "ingredient_" + random.Next(100000, 999999);
                if (!realIngredients.Contains(fakeWord))
                {
                    missingIngredients.Add(fakeWord);
                }
            }

            // 3. Tests
            var configs = new[]
            {
                new { Size = 1000, Hashes = 3 },
                new { Size = 5000, Hashes = 3 },
                new { Size = 10000, Hashes = 5 }
            };

            foreach (var config in configs)
            {
                var filter = new BloomFilter(config.Size, config.Hashes);
                foreach (var ing in realIngredients)
                {
                    filter.Add(ing);
                }

                int falsePositives = 0;
                foreach (var missing in missingIngredients)
                {
                    if (filter.MightContain(missing))
                    {
                        falsePositives++;
                    }
                }

                results.Add(new BloomFilterBenchmarkResult
                {
                    FilterSize = config.Size,
                    HashCount = config.Hashes,
                    FalsePositives = falsePositives,
                    FalsePositiveRate = falsePositives / 1000.0
                });
            }

            return results;
        }
    }
}
