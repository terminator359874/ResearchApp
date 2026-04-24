using System;
using Xunit;
using FluentAssertions;
using RecipeSearchApp.Services;

namespace RecipeSearchApp.Tests.Services
{
    public class BloomFilterTests
    {
        [Fact]
        public void Add_SingleItem_ShouldBeDetectable()
        {
            var filter = new BloomFilter(size: 1000, hashFunctionCount: 3);
            var item = "картофель";

            filter.Add(item);

            filter.MightContain(item).Should().BeTrue();
        }

        [Fact]
        public void MightContain_NonExistentItem_ShouldReturnFalse()
        {
            var filter = new BloomFilter();
            filter.Add("морковь");

            var result = filter.MightContain("капуста");

            result.Should().BeFalse();
        }

        [Fact]
        public void Add_MultipleItems_AllShouldBeDetectable()
        {
            var filter = new BloomFilter();
            var items = new[] { "лук", "чеснок", "перец", "соль" };

            foreach (var item in items)
                filter.Add(item);

            foreach (var item in items)
                filter.MightContain(item).Should().BeTrue();
        }

        [Fact]
        public void MightContain_CaseInsensitive_ShouldWork()
        {
            var filter = new BloomFilter();
            filter.Add("Томат");

            filter.MightContain("томат").Should().BeTrue();
            filter.MightContain("ТОМАТ").Should().BeTrue();
        }

        [Fact]
        public void Clear_AfterAdding_ShouldRemoveAll()
        {
            var filter = new BloomFilter();
            filter.Add("сахар");
            filter.Add("соль");

            filter.Clear();

            filter.MightContain("сахар").Should().BeFalse();
            filter.MightContain("соль").Should().BeFalse();
        }

        [Fact]
        public void FalsePositiveRate_ShouldBeLow()
        {
            var filter = new BloomFilter(size: 10000, hashFunctionCount: 3);
            var testItems = new[] { "item1", "item2", "item3", "item4", "item5" };

            foreach (var item in testItems)
                filter.Add(item);

            var falsePositives = 0;
            for (int i = 0; i < 100; i++)
            {
                var testWord = $"nonexistent_{i}_{Guid.NewGuid()}";
                if (filter.MightContain(testWord))
                    falsePositives++;
            }

            falsePositives.Should().BeLessThan(10);
        }
    }
}
