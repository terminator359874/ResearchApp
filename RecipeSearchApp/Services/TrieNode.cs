using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeSearchApp.Services
{
    public class TrieNode
    {
        public Dictionary<char, TrieNode> Children { get; } = new();

        public bool IsEndOfWord { get; set; }

        public string? Word { get; set; }
    }

    public class Trie
    {
        private readonly TrieNode _root = new();

        public void Insert(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return;

            var current = _root;
            var lowerWord = word.ToLowerInvariant();

            foreach (var ch in lowerWord)
            {
                if (!current.Children.ContainsKey(ch))
                    current.Children[ch] = new TrieNode();

                current = current.Children[ch];
            }

            current.IsEndOfWord = true;
            current.Word = word;
        }

        public List<string> SearchByPrefix(string prefix, int maxResults = 10)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return new List<string>();

            var results = new List<string>();
            var current = _root;
            var lowerPrefix = prefix.ToLowerInvariant();

            foreach (var ch in lowerPrefix)
            {
                if (!current.Children.ContainsKey(ch))
                    return results;

                current = current.Children[ch];
            }

            CollectWords(current, results, maxResults);
            return results;
        }

        private void CollectWords(TrieNode node, List<string> results, int maxResults)
        {
            if (results.Count >= maxResults)
                return;

            if (node.IsEndOfWord && node.Word != null)
                results.Add(node.Word);

            foreach (var child in node.Children.Values)
                CollectWords(child, results, maxResults);
        }

        public void Clear()
        {
            _root.Children.Clear();
        }
    }
}
