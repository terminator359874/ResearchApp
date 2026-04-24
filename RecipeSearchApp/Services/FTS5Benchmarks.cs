using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeSearchApp.Services
{
    public class FTS5BenchmarkResult
    {
        public string QueryType { get; set; } = string.Empty;
        public string Syntax { get; set; } = string.Empty;
        public int ResultCount { get; set; }
        public double TimeMs { get; set; }
    }

    public class FTS5Benchmarks
    {
        public static async Task<List<FTS5BenchmarkResult>> RunFTS5TestsAsync(DatabaseService db)
        {
            var results = new List<FTS5BenchmarkResult>();
            var queries = new[]
            {
                new { Type = "Простой поиск", Query = "картофель" },
                new { Type = "Поиск фразы", Query = "\"томатная паста\"" },
                new { Type = "Логическое И", Query = "морковь AND лук" },
                new { Type = "Логическое ИЛИ", Query = "рис OR гречка" },
                new { Type = "Исключение", Query = "суп NOT томат" },
                new { Type = "Префиксный поиск", Query = "карто*" },
                new { Type = "Поиск в поле", Query = "Title:борщ" },
                new { Type = "Близость слов", Query = "NEAR(картофель морковь, 5)" }
            };

            foreach (var q in queries)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                // To avoid first-run caching effect, let's run it once ignoring time, then measure
                var resStr = await db.FullTextSearchAsync(q.Query);
                
                sw.Restart();
                var res = await db.FullTextSearchAsync(q.Query);
                sw.Stop();

                results.Add(new FTS5BenchmarkResult
                {
                    QueryType = q.Type,
                    Syntax = q.Query,
                    ResultCount = res.Count,
                    TimeMs = Math.Round(sw.Elapsed.TotalMilliseconds, 4)
                });
            }

            return results;
        }
    }
}
