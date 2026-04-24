using System;
using System.Collections.Generic;
using System.Linq;

namespace RecipeSearchApp.Services
{
    public class CompressedTrieNode
    {
        public Dictionary<char, CompressedTrieNode> Children { get; set; } = new();
        public string EdgeLabel { get; set; } = string.Empty;
        public bool IsEndOfWord { get; set; }
        public string? Word { get; set; }
    }

    public class CompressedTrie
    {
        private readonly CompressedTrieNode _root = new();

        public void Insert(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return;
                
            var lowerWord = word.ToLowerInvariant();
            InsertRecursive(_root, lowerWord, word, lowerWord);
        }

        private void InsertRecursive(CompressedTrieNode node, string currentSuffix, string originalWord, string lowerWord)
        {
            if (string.IsNullOrEmpty(currentSuffix))
            {
                node.IsEndOfWord = true;
                node.Word = originalWord;
                return;
            }

            char firstChar = currentSuffix[0];
            if (!node.Children.TryGetValue(firstChar, out var child))
            {
                child = new CompressedTrieNode
                {
                    EdgeLabel = currentSuffix,
                    IsEndOfWord = true,
                    Word = originalWord
                };
                node.Children[firstChar] = child;
                return;
            }

            int commonLen = 0;
            while (commonLen < currentSuffix.Length && commonLen < child.EdgeLabel.Length && currentSuffix[commonLen] == child.EdgeLabel[commonLen])
            {
                commonLen++;
            }

            if (commonLen == child.EdgeLabel.Length)
            {
                InsertRecursive(child, currentSuffix.Substring(commonLen), originalWord, lowerWord);
            }
            else
            {
                var splitNode = new CompressedTrieNode
                {
                    EdgeLabel = child.EdgeLabel.Substring(0, commonLen)
                };

                node.Children[firstChar] = splitNode;

                child.EdgeLabel = child.EdgeLabel.Substring(commonLen);
                splitNode.Children[child.EdgeLabel[0]] = child;

                if (commonLen == currentSuffix.Length)
                {
                    splitNode.IsEndOfWord = true;
                    splitNode.Word = originalWord;
                }
                else
                {
                    var newLeaf = new CompressedTrieNode
                    {
                        EdgeLabel = currentSuffix.Substring(commonLen),
                        IsEndOfWord = true,
                        Word = originalWord
                    };
                    splitNode.Children[newLeaf.EdgeLabel[0]] = newLeaf;
                }
            }
        }

        public List<string> SearchByPrefix(string prefix, int maxResults = 10)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return new List<string>();

            var lowerPrefix = prefix.ToLowerInvariant();
            var results = new List<string>();
            var (node, remainingPrefix) = FindNodePos(_root, lowerPrefix);

            if (node == null)
                return results;

            CollectWords(node, results, maxResults);
            
            return results;
        }

        private (CompressedTrieNode? node, string remaining) FindNodePos(CompressedTrieNode node, string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return (node, string.Empty);

            if (!node.Children.TryGetValue(prefix[0], out var child))
                return (null, prefix);

            int commonLen = 0;
            while (commonLen < prefix.Length && commonLen < child.EdgeLabel.Length && prefix[commonLen] == child.EdgeLabel[commonLen])
            {
                commonLen++;
            }

            if (commonLen == prefix.Length)
            {
                return (child, string.Empty);
            }

            if (commonLen == child.EdgeLabel.Length)
            {
                return FindNodePos(child, prefix.Substring(commonLen));
            }

            return (null, prefix.Substring(commonLen)); 
        }

        private void CollectWords(CompressedTrieNode node, List<string> results, int maxResults)
        {
            if (results.Count >= maxResults)
                return;

            if (node.IsEndOfWord && node.Word != null)
                results.Add(node.Word);

            foreach (var child in node.Children.Values)
                CollectWords(child, results, maxResults);
        }

        public (int nodeCount, int estBytes) AnalyzeMemoryUsage()
        {
            int nodes = 0;
            int edgeLabelChars = 0;
            CountMemory(_root, ref nodes, ref edgeLabelChars);
            
            int bytes = nodes * 64 + edgeLabelChars * 2;
            return (nodes, bytes);
        }

        private void CountMemory(CompressedTrieNode node, ref int nodes, ref int characters)
        {
            nodes++;
            characters += node.EdgeLabel?.Length ?? 0;
            foreach (var child in node.Children.Values)
            {
                CountMemory(child, ref nodes, ref characters);
            }
        }
    }

    public class TrieBenchmarkResult
    {
        public string Type { get; set; } = string.Empty;
        public int WordCount { get; set; }
        public int NodeCount { get; set; }
        public int EstMemoryBytes { get; set; }
        public double SearchTimeMs { get; set; }
    }

    public class TrieMemoryBenchmarks
    {
        public static List<TrieBenchmarkResult> BenchmarkTries()
        {
            var results = new List<TrieBenchmarkResult>();
            var wordCounts = new[] { 100, 500, 1000 };
            
            var rand = new Random(42);
            var baseWords = new List<string>();
            for (int i = 0; i < 1000; i++)
            {
                char c1 = (char)rand.Next('a', 'z');
                char c2 = (char)rand.Next('a', 'z');
                baseWords.Add("word_" + c1 + c2 + rand.Next(1, 10000).ToString());
            }

            foreach (var wc in wordCounts)
            {
                var dict = baseWords.Take(wc).ToList();

                var trie = new Trie();
                var compTrie = new CompressedTrie();

                foreach (var w in dict)
                {
                    trie.Insert(w);
                    compTrie.Insert(w);
                }

                int tNodes = 0;
                int tChars = 0;
                CountStandardTrie(trie, ref tNodes, ref tChars);
                int tBytes = tNodes * 64; 

                var searchPrefix = "word_a";
                var sw = System.Diagnostics.Stopwatch.StartNew();
                for (int i = 0; i < 1000; i++) { trie.SearchByPrefix(searchPrefix); }
                sw.Stop();
                var stdTime = sw.Elapsed.TotalMilliseconds;

                results.Add(new TrieBenchmarkResult
                {
                    Type = "Trie",
                    WordCount = wc,
                    NodeCount = tNodes,
                    EstMemoryBytes = tBytes,
                    SearchTimeMs = stdTime
                });

                var (cNodes, cBytes) = compTrie.AnalyzeMemoryUsage();
                sw.Restart();
                for (int i = 0; i < 1000; i++) { compTrie.SearchByPrefix(searchPrefix); }
                sw.Stop();
                var cTime = sw.Elapsed.TotalMilliseconds;

                results.Add(new TrieBenchmarkResult
                {
                    Type = "Compressed",
                    WordCount = wc,
                    NodeCount = cNodes,
                    EstMemoryBytes = cBytes,
                    SearchTimeMs = cTime
                });
            }

            return results;
        }

        private static void CountStandardTrie(Trie trie, ref int nodes, ref int chars)
        {
            var field = typeof(Trie).GetField("_root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(trie) is TrieNode root)
            {
                CountStdInternal(root, ref nodes, ref chars);
            }
        }

        private static void CountStdInternal(TrieNode node, ref int nodes, ref int chars)
        {
            nodes++;
            foreach(var child in node.Children.Values)
            {
                CountStdInternal(child, ref nodes, ref chars);
            }
        }
    }
}
