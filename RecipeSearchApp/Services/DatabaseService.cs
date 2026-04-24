using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using RecipeSearchApp.Models;

namespace RecipeSearchApp.Services;

public class DatabaseService : IDatabaseService
{
    private readonly string _dbPath;
    private readonly Trie _titleTrie = new();
    private readonly BloomFilter _ingredientFilter = new();
    private readonly InMemoryCache _cache = new();

    public DatabaseService(string? customDbPath = null)
    {
        if (customDbPath != null)
        {
            _dbPath = customDbPath;
        }
        else
        {
            var folder = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
            _dbPath = Path.Combine(folder, "recipes.db");
        }
    }

    // отдельный метод
    public async Task InitializeAsync()
    {
        await InitializeDatabaseAsync();
        
        var count = await GetRecipeCountAsync();
        if (count <= 5)
        {
            await AddMultipleRecipesAsync();
        }
    }

    private async Task InitializeDatabaseAsync()
    {
        await Task.Run(() =>
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            // Основная таблица
            var createTable = @"
                CREATE TABLE IF NOT EXISTS Recipes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Ingredients TEXT NOT NULL,
                    Instructions TEXT NOT NULL,
                    Category TEXT NOT NULL,
                    PrepTimeMinutes INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    ViewCount INTEGER DEFAULT 0,
                    AverageRating REAL DEFAULT 0,
                    RatingCount INTEGER DEFAULT 0
                )";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = createTable;
                command.ExecuteNonQuery();
            }

            // FTS5 таблица
            var createFts = @"
                CREATE VIRTUAL TABLE IF NOT EXISTS RecipesFts USING fts5(
                    Title,
                    Ingredients,
                    Instructions,
                    content='Recipes',
                    content_rowid='Id'
                )";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = createFts;
                command.ExecuteNonQuery();
            }

            // Триггеры
            var triggers = new[]
            {
                @"
                CREATE TRIGGER IF NOT EXISTS recipes_ai
                AFTER INSERT ON Recipes
                BEGIN
                    INSERT INTO RecipesFts(rowid, Title, Ingredients, Instructions)
                    VALUES (new.Id, new.Title, new.Ingredients, new.Instructions);
                END",

                @"
                CREATE TRIGGER IF NOT EXISTS recipes_ad
                AFTER DELETE ON Recipes
                BEGIN
                    DELETE FROM RecipesFts WHERE rowid = old.Id;
                END",

                @"
                CREATE TRIGGER IF NOT EXISTS recipes_au
                AFTER UPDATE ON Recipes
                BEGIN
                    UPDATE RecipesFts
                    SET
                        Title = new.Title,
                        Ingredients = new.Ingredients,
                        Instructions = new.Instructions
                    WHERE rowid = new.Id;
                END"
            };

            foreach (var trigger in triggers)
            {
                using var command = connection.CreateCommand();
                command.CommandText = trigger;
                command.ExecuteNonQuery();
            }

            // Проверка на пустую БД
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM Recipes";
                var count = (long)(command.ExecuteScalar() ?? 0L);

                if (count == 0)
                {
                    AddSampleRecipes(connection);
                }
            }

            // Перестроение поисковых структур
            RebuildSearchStructures(connection);
        });
    }

    private void AddSampleRecipes(SqliteConnection connection)
    {
        var samples = new[]
        {
            (
                "Борщ классический",
                "свёкла, капуста, картофель, морковь, лук, томатная паста, говядина",
                "Отварить говядину. Нарезать овощи. Добавить в бульон поочередно.",
                "Супы",
                90
            ),
            (
                "Оливье",
                "картофель, морковь, яйца, огурцы солёные, горошек, колбаса, майонез",
                "Отварить картофель и морковь. Нарезать кубиками все ингредиенты. Заправить майонезом.",
                "Салаты",
                30
            ),
            (
                "Блины тонкие",
                "мука, молоко, яйца, сахар, соль, масло растительное",
                "Смешать яйца с сахаром. Добавить молоко и муку. Жарить на сковороде.",
                "Десерты",
                25
            ),
            (
                "Плов узбекский",
                "рис, баранина, морковь, лук, масло, зира, барбарис",
                "Обжарить мясо с луком. Добавить морковь. Засыпать рис. Тушить под крышкой.",
                "Основные блюда",
                120
            ),
            (
                "Цезарь с курицей",
                "салат романо, курица, сухарики, пармезан, соус цезарь",
                "Обжарить курицу. Нарвать салат. Смешать с сухариками и заправить соусом.",
                "Салаты",
                20
            )
        };

        using var transaction = connection.BeginTransaction();

        foreach (var (title, ingredients, instructions, category, time) in samples)
        {
            using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO Recipes
                    (Title, Ingredients, Instructions, Category, PrepTimeMinutes, CreatedAt)
                VALUES
                    (@title, @ingredients, @instructions, @category, @time, @created)";

            command.Parameters.AddWithValue("@title", title);
            command.Parameters.AddWithValue("@ingredients", ingredients);
            command.Parameters.AddWithValue("@instructions", instructions);
            command.Parameters.AddWithValue("@category", category);
            command.Parameters.AddWithValue("@time", time);
            command.Parameters.AddWithValue("@created", DateTime.Now.ToString("O"));

            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private void RebuildSearchStructures(SqliteConnection connection)
    {
        _titleTrie.Clear();
        _ingredientFilter.Clear();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Title, Ingredients FROM Recipes";

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var title = reader.GetString(0);
            var ingredients = reader.GetString(1);

            _titleTrie.Insert(title);

            foreach (var ingredient in ingredients.Split(',', StringSplitOptions.TrimEntries))
            {
                _ingredientFilter.Add(ingredient);
            }
        }
    }

    public async Task<List<Recipe>> FullTextSearchAsync(string query)
    {
        return await Task.Run(() =>
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT r.*, bm25(RecipesFts) as rank
                FROM Recipes r
                JOIN RecipesFts ON RecipesFts.rowid = r.Id
                WHERE RecipesFts MATCH @query
                ORDER BY rank
                LIMIT 50";

            command.Parameters.AddWithValue("@query", query);

            var results = new List<Recipe>();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                results.Add(ReadRecipe(reader));
            }

            return results;
        });
    }

    public List<string> GetTitleSuggestions(string prefix)
    {
        return _titleTrie.SearchByPrefix(prefix);
    }

    public bool MightHaveIngredient(string ingredient)
    {
        return _ingredientFilter.MightContain(ingredient);
    }

    public async Task<Recipe?> GetRecipeByIdAsync(int id)
    {
        var cached = _cache.Get(id);
        if (cached != null)
            return cached;

        return await Task.Run(() =>
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Recipes WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                var recipe = ReadRecipe(reader);

                using var updateCommand = connection.CreateCommand();
                updateCommand.CommandText = @"
                    UPDATE Recipes
                    SET ViewCount = ViewCount + 1
                    WHERE Id = @id";

                updateCommand.Parameters.AddWithValue("@id", id);
                updateCommand.ExecuteNonQuery();

                recipe.ViewCount++;

                _cache.Set(id, recipe);

                return recipe;
            }

            return null;
        });
    }

    public async Task<List<Recipe>> GetAllRecipesAsync()
    {
        return await Task.Run(() =>
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Recipes ORDER BY CreatedAt DESC";

            var results = new List<Recipe>();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                results.Add(ReadRecipe(reader));
            }

            return results;
        });
    }

    public async Task AddRecipeAsync(Recipe recipe)
    {
        await Task.Run(() =>
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Recipes
                    (Title, Ingredients, Instructions, Category, PrepTimeMinutes, CreatedAt)
                VALUES
                    (@title, @ingredients, @instructions, @category, @time, @created)";

            command.Parameters.AddWithValue("@title", recipe.Title);
            command.Parameters.AddWithValue("@ingredients", recipe.Ingredients);
            command.Parameters.AddWithValue("@instructions", recipe.Instructions);
            command.Parameters.AddWithValue("@category", recipe.Category);
            command.Parameters.AddWithValue("@time", recipe.PrepTimeMinutes);
            command.Parameters.AddWithValue("@created", DateTime.Now.ToString("O"));

            command.ExecuteNonQuery();

            _titleTrie.Insert(recipe.Title);

            foreach (var ingredient in recipe.Ingredients.Split(',', StringSplitOptions.TrimEntries))
            {
                _ingredientFilter.Add(ingredient);
            }
        });
    }

    public Dictionary<string, object> GetCacheStatistics()
    {
        return _cache.GetStatistics();
    }

    public async Task<int> GetRecipeCountAsync()
    {
        return await Task.Run(() =>
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Recipes";
            return Convert.ToInt32(command.ExecuteScalar());
        });
    }

    public async Task AddMultipleRecipesAsync()
    {
        var newRecipes = new List<Recipe>
        {
            // 5 Супов
            new Recipe { Title = "Солянка мясная сборная", Ingredients = "говядина, колбаса, сосиски, огурцы соленые, томатная паста, маслины, лимон", Instructions = "Сварить бульон. Обжарить колбасу и лук с томатной пастой. Добавить нарезанные огурцы и мясо в бульон. Варить 15 минут.", Category = "Супы", PrepTimeMinutes = 60, CreatedAt = DateTime.Now },
            new Recipe { Title = "Грибной крем-суп", Ingredients = "шампиньоны, картофель, сливки, лук, чеснок, сливочное масло", Instructions = "Обжарить грибы с луком и чесноком. Отварить картофель. Соединить, пюрировать блендером. Добавить сливки и довести до кипения.", Category = "Супы", PrepTimeMinutes = 40, CreatedAt = DateTime.Now },
            new Recipe { Title = "Суп Харчо", Ingredients = "говядина, рис, грецкие орехи, ткемали, хмели-сунели, кинза", Instructions = "Сварить бульон. Добавить рис. Обжарить лук с орехами и специями. Добавить в суп за 10 мин до готовности. Заправить ткемали и кинзой.", Category = "Супы", PrepTimeMinutes = 90, CreatedAt = DateTime.Now },
            new Recipe { Title = "Уха из красной рыбы", Ingredients = "лосось, картофель, морковь, лук, перец горошком, лавровый лист", Instructions = "Сварить рыбный бульон. Процедить. Добавить нарезанные овощи и филе рыбы. Варить 20 минут.", Category = "Супы", PrepTimeMinutes = 35, CreatedAt = DateTime.Now },
            new Recipe { Title = "Томатный суп Гаспачо", Ingredients = "томаты, огурцы, перец болгарский, чеснок, оливковое масло, винный уксус", Instructions = "Ошпарить томаты, снять кожицу. Пюрировать все овощи в блендере. Заправить маслом и уксусом. Охладить перед подачей.", Category = "Супы", PrepTimeMinutes = 20, CreatedAt = DateTime.Now },
            
            // 5 Салатов
            new Recipe { Title = "Греческий салат", Ingredients = "помидоры, огурцы, перец сыр фета, маслины, красный лук, оливковое масло, орегано", Instructions = "Крупно нарезать овощи. Добавить маслины и кубики феты. Заправить оливковым маслом с орегано.", Category = "Салаты", PrepTimeMinutes = 15, CreatedAt = DateTime.Now },
            new Recipe { Title = "Салат Нисуаз", Ingredients = "тунец консервированный, яйца, стручковая фасоль, картофель, помидоры черри, анчоусы", Instructions = "Отварить яйца, картофель и фасоль. Выложить на тарелку листья салата, овощи, тунец. Заправить соусом на основе горчицы.", Category = "Салаты", PrepTimeMinutes = 25, CreatedAt = DateTime.Now },
            new Recipe { Title = "Салат Винегрет", Ingredients = "свёкла, картофель, морковь, соленые огурцы, квашеная капуста, зеленый горошек, масло", Instructions = "Отварить корнеплоды. Нарезать кубиками. Смешать с капустой, огурцами и горошком. Заправить маслом.", Category = "Салаты", PrepTimeMinutes = 40, CreatedAt = DateTime.Now },
            new Recipe { Title = "Салат Мимоза", Ingredients = "консервированная горбуша, яйца, сыр, сливочное масло, лук, майонез", Instructions = "Выкладывать слоями: рыба, белки, сыр, масло, желтки. Промазывать слои майонезом.", Category = "Салаты", PrepTimeMinutes = 35, CreatedAt = DateTime.Now },
            new Recipe { Title = "Салат Капрезе", Ingredients = "помидоры, моцарелла, свежий базилик, бальзамический крем, оливковое масло", Instructions = "Нарезать помидоры и моцареллу кружочками. Выложить чередуя. Украсить базиликом, полить маслом и кремом.", Category = "Салаты", PrepTimeMinutes = 10, CreatedAt = DateTime.Now },

            // 5 Основных блюд
            new Recipe { Title = "Паста Карбонара", Ingredients = "спагетти, гуанчиале (или бекон), яйца (желтки), сыр пекорино, черный перец", Instructions = "Отварить пасту. Обжарить бекон. Смешать желтки с тертым сыром и перцем. Соединить пасту с беконом и яичным соусом (вне огня).", Category = "Основные блюда", PrepTimeMinutes = 25, CreatedAt = DateTime.Now },
            new Recipe { Title = "Запеченная курица с картофелем", Ingredients = "курица целиком, картофель, чеснок, розмарин, паприка, оливковое масло", Instructions = "Натереть курицу специями и маслом. Выложить в форму. Вокруг разложить нарезанный картофель с розмарином. Запекать 1.5 часа при 180°C.", Category = "Основные блюда", PrepTimeMinutes = 100, CreatedAt = DateTime.Now },
            new Recipe { Title = "Стейк Рибай", Ingredients = "говяжий стейк рибай, соль, перец, сливочное масло, чеснок, тимьян", Instructions = "Достать стейк заранее (комнатная температура). Обжарить на сильном огне по 2-3 минуты с каждой стороны. Добавить масло, чеснок и тимьян, поливать стейк маслом. Дать отдохнуть 5 минут.", Category = "Основные блюда", PrepTimeMinutes = 20, CreatedAt = DateTime.Now },
            new Recipe { Title = "Котлеты по-киевски", Ingredients = "куриное филе, сливочное масло, укроп, яйца, панировочные сухари, масло для фритюра", Instructions = "Отбить филе. Завернуть внутрь замороженное масло с укропом. Дважды обвалять в яйце и сухарях. Жарить во фритюре, затем довести в духовке.", Category = "Основные блюда", PrepTimeMinutes = 60, CreatedAt = DateTime.Now },
            new Recipe { Title = "Ризотто с грибами", Ingredients = "рис арборио, белые грибы, лук шалот, белое вино, пармезан, бульон, сливочное масло", Instructions = "Обжарить лук и грибы. Добавить рис, влить вино. Постепенно подливать горячий бульон. В конце вмешать масло и сыр.", Category = "Основные блюда", PrepTimeMinutes = 40, CreatedAt = DateTime.Now },

            // 5 Десертов
            new Recipe { Title = "Тирамису", Ingredients = "печенье савоярди, маскарпоне, яйца, сахар, кофе эспрессо, какао", Instructions = "Взбить желтки с сахаром, добавить маскарпоне. Взбить белки. Аккуратно соединить. Обмакивать печенье в кофе. Выкладывать слоями крем и печенье. Посыпать какао.", Category = "Десерты", PrepTimeMinutes = 30, CreatedAt = DateTime.Now },
            new Recipe { Title = "Чизкейк Нью-Йорк", Ingredients = "сыр филадельфия, сливки, яйца, сахар, песочное печенье, сливочное масло", Instructions = "Измельчить печенье с маслом, утрамбовать в форму. Взбить сыр со сливками, яйцами и сахаром. Выпекать на водяной бане 1 час, затем охладить 8 часов.", Category = "Десерты", PrepTimeMinutes = 90, CreatedAt = DateTime.Now },
            new Recipe { Title = "Шоколадный фондан", Ingredients = "горький шоколад, сливочное масло, сахар, яйца, мука", Instructions = "Растопить шоколад с маслом. Взбить яйца с сахаром. Вмешать муку и шоколадную массу. Выпекать в формах 10-12 минут при 200°C (внутри должен остаться жидким).", Category = "Десерты", PrepTimeMinutes = 25, CreatedAt = DateTime.Now },
            new Recipe { Title = "Панна-котта", Ingredients = "сливки, молоко, сахар, желатин, ваниль, ягоды", Instructions = "Замочить желатин. Нагреть сливки с молоком, сахаром и ванилью (не доводя до кипения). Растворить желатин. Разлить по формам, охладить. Подавать с ягодным соусом.", Category = "Десерты", PrepTimeMinutes = 120, CreatedAt = DateTime.Now },
            new Recipe { Title = "Панкейки", Ingredients = "молоко, мука, яйца, сливочное масло, сахар, разрыхлитель", Instructions = "Смешать сухие ингредиенты. Отдельно смешать жидкие. Соединить (не вымешивая долго). Жарить на сухой сковороде до появления пузырьков.", Category = "Десерты", PrepTimeMinutes = 20, CreatedAt = DateTime.Now }
        };

        foreach (var recipe in newRecipes)
        {
            await AddRecipeAsync(recipe);
        }
    }

    public async Task AddRatingAsync(int recipeId, int rating)
    {
        await Task.Run(() =>
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            using var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT AverageRating, RatingCount FROM Recipes WHERE Id = @id";
            selectCmd.Parameters.AddWithValue("@id", recipeId);

            using var reader = selectCmd.ExecuteReader();
            if (!reader.Read()) return;

            var currentAvg = reader.GetDouble(0);
            var currentCount = reader.GetInt32(1);
            reader.Close();

            var newCount = currentCount + 1;
            var newAvg = ((currentAvg * currentCount) + rating) / newCount;

            using var updateCmd = connection.CreateCommand();
            updateCmd.CommandText = @"
                UPDATE Recipes
                SET AverageRating = @avg, RatingCount = @count
                WHERE Id = @id";
            updateCmd.Parameters.AddWithValue("@avg", newAvg);
            updateCmd.Parameters.AddWithValue("@count", newCount);
            updateCmd.Parameters.AddWithValue("@id", recipeId);
            updateCmd.ExecuteNonQuery();
        });
    }

    private static Recipe ReadRecipe(SqliteDataReader reader)
    {
        return new Recipe
        {
            Id = reader.GetInt32(0),
            Title = reader.GetString(1),
            Ingredients = reader.GetString(2),
            Instructions = reader.GetString(3),
            Category = reader.GetString(4),
            PrepTimeMinutes = reader.GetInt32(5),
            CreatedAt = DateTime.Parse(reader.GetString(6)),
            ViewCount = reader.GetInt32(7),
            AverageRating = reader.IsDBNull(8) ? 0 : reader.GetDouble(8),
            RatingCount = reader.IsDBNull(9) ? 0 : reader.GetInt32(9)
        };
    }
}
