using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace RecipeSearchApp.Services
{
    public class BloomFilter
    {
        private readonly BitArray _bits;
        private readonly int _hashFunctionCount;

        public BloomFilter(int size = 10000, int hashFunctionCount = 3)
        {
            _bits = new BitArray(size);
            _hashFunctionCount = hashFunctionCount;
        }

        public void Add(string item)
        {
            if (string.IsNullOrWhiteSpace(item))
                return;

            var lowerItem = item.ToLowerInvariant();

            foreach (var hash in GetHashes(lowerItem))
                _bits[hash] = true;
        }

        public bool MightContain(string item)
        {
            if (string.IsNullOrWhiteSpace(item))
                return false;

            var lowerItem = item.ToLowerInvariant();

            foreach (var hash in GetHashes(lowerItem))
            {
                if (!_bits[hash])
                    return false;
            }

            return true;
        }

        private int[] GetHashes(string item)
        {
            var hashes = new int[_hashFunctionCount];
            var baseHash = item.GetHashCode();

            for (var i = 0; i < _hashFunctionCount; i++)
            {
                var hash = (baseHash ^ (i * 0x9E3779B9)) & 0x7FFFFFFF;
                hashes[i] = (int)(hash % _bits.Length);
            }

            return hashes;
        }

        public void Clear()
        {
            _bits.SetAll(false);
        }
    }
}
