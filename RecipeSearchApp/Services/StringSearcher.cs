using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeSearchApp.Services
{
    public class StringSearcher
    {
        public static List<int> KnuthMorrisPratt(string text, string pattern)
        {
            var results = new List<int>();

            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
                return results;

            var lowerText = text.ToLowerInvariant();
            var lowerPattern = pattern.ToLowerInvariant();

            var lps = ComputeLPSArray(lowerPattern);

            int i = 0, j = 0;

            while (i < lowerText.Length)
            {
                if (lowerText[i] == lowerPattern[j])
                {
                    i++;
                    j++;
                }

                if (j == lowerPattern.Length)
                {
                    results.Add(i - j);
                    j = lps[j - 1];
                }
                else if (i < lowerText.Length && lowerText[i] != lowerPattern[j])
                {
                    if (j != 0)
                        j = lps[j - 1];
                    else
                        i++;
                }
            }

            return results;
        }

        private static int[] ComputeLPSArray(string pattern)
        {
            var lps = new int[pattern.Length];

            int length = 0;
            int i = 1;

            while (i < pattern.Length)
            {
                if (pattern[i] == pattern[length])
                {
                    length++;
                    lps[i] = length;
                    i++;
                }
                else
                {
                    if (length != 0)
                        length = lps[length - 1];
                    else
                    {
                        lps[i] = 0;
                        i++;
                    }
                }
            }

            return lps;
        }

        public static List<int> BoyerMoore(string text, string pattern)
        {
            var results = new List<int>();

            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
                return results;

            var lowerText = text.ToLowerInvariant();
            var lowerPattern = pattern.ToLowerInvariant();

            var badChar = BuildBadCharTable(lowerPattern);
            var s = 0;

            while (s <= lowerText.Length - lowerPattern.Length)
            {
                var j = lowerPattern.Length - 1;

                while (j >= 0 && lowerPattern[j] == lowerText[s + j])
                    j--;

                if (j < 0)
                {
                    results.Add(s);

                    s += (s + lowerPattern.Length < lowerText.Length)
                        ? lowerPattern.Length - badChar.GetValueOrDefault(lowerText[s + lowerPattern.Length], -1)
                        : 1;
                }
                else
                {
                    s += Math.Max(1, j - badChar.GetValueOrDefault(lowerText[s + j], -1));
                }
            }

            return results;
        }

        private static Dictionary<char, int> BuildBadCharTable(string pattern)
        {
            var table = new Dictionary<char, int>();

            for (var i = 0; i < pattern.Length - 1; i++)
                table[pattern[i]] = i;

            return table;
        }

        public static string HighlightMatches(string text, string pattern, string algorithm = "KMP")
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
                return text;

            var positions = algorithm.ToUpper() == "BM"
                ? BoyerMoore(text, pattern)
                : KnuthMorrisPratt(text, pattern);

            if (positions.Count == 0)
                return text;

            var highlighted = text;

            for (var i = positions.Count - 1; i >= 0; i--)
            {
                var pos = positions[i];
                highlighted = highlighted.Insert(pos + pattern.Length, "】");
                highlighted = highlighted.Insert(pos, "【");
            }

            return highlighted;
        }

        public static List<BenchmarkResult> BenchmarkSearchAlgorithms()
        {
            var results = new List<BenchmarkResult>();
            var longText = new StringBuilder();
            for (int i = 0; i < 50; i++)
            {
                longText.Append("Это длинный текст для тестирования алгоритмов поиска подстроки. Он содержит множество слов и символов. ");
            }
            longText.Append("СекретныйПаттернДляПоиска тут. ");
            for (int i = 0; i < 50; i++)
            {
                longText.Append("Это длинный текст для тестирования алгоритмов поиска подстроки. Он содержит множество слов и символов. ");
            }
            var text = longText.ToString();

            var testCases = new[]
            {
                new { Name = "Короткий паттерн (3-5 символов)", Text = text, Pattern = "для" },
                new { Name = "Длинный паттерн (20-30 символов)", Text = text, Pattern = "СекретныйПаттернДляПоиска" },
                new { Name = "Повторяющиеся символы", Text = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", Pattern = "aaaa" },
                new { Name = "Паттерн отсутствует", Text = text, Pattern = "НесуществующийТекст12345" },
                new { Name = "Множественные вхождения", Text = text, Pattern = "алгоритмов" }
            };

            foreach (var testCase in testCases)
            {
                double kmpTotal = 0;
                double bmTotal = 0;
                int runs = 100;

                for (int i = 0; i < runs; i++)
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    KnuthMorrisPratt(testCase.Text, testCase.Pattern);
                    sw.Stop();
                    kmpTotal += sw.Elapsed.TotalMilliseconds;

                    sw.Restart();
                    BoyerMoore(testCase.Text, testCase.Pattern);
                    sw.Stop();
                    bmTotal += sw.Elapsed.TotalMilliseconds;
                }

                var kmpAvg = kmpTotal / runs;
                var bmAvg = bmTotal / runs;

                results.Add(new BenchmarkResult
                {
                    TestName = testCase.Name,
                    Pattern = testCase.Pattern,
                    KMPTimeMs = Math.Round(kmpAvg, 4),
                    BMTimeMs = Math.Round(bmAvg, 4),
                    FasterAlgorithm = kmpAvg < bmAvg ? "КМП" : "БМ"
                });
            }

            return results;
        }
    }

    public class BenchmarkResult
    {
        public string TestName { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public double KMPTimeMs { get; set; }
        public double BMTimeMs { get; set; }
        public string FasterAlgorithm { get; set; } = string.Empty;
    }
}
