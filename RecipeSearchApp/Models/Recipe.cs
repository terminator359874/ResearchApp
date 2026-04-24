using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeSearchApp.Models
{
    public class Recipe
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Ingredients { get; set; } = string.Empty;

        public string Instructions { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public int PrepTimeMinutes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Для кеширования популярных рецептов
        public int ViewCount { get; set; }

        // Новые поля для рейтинга
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
    }
}
