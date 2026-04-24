using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using Moq;
using RecipeSearchApp.ViewModels;
using RecipeSearchApp.Models;
using RecipeSearchApp.Services;

namespace RecipeSearchApp.Tests.ViewModels
{
    public class MainViewModelTests
    {
        [Fact]
        public void SearchQuery_WhenChanged_ShouldUpdateSuggestions()
        {
            var mockDatabase = new Mock<IDatabaseService>();
            mockDatabase
                .Setup(db => db.GetTitleSuggestions(It.IsAny<string>()))
                .Returns(new List<string> { "Борщ", "Блины" });

            var viewModel = new MainViewModel(mockDatabase.Object);
            
            viewModel.SearchQuery = "Б";

            // Length needs to be >= 2 for suggestions to update, let's update test 
            viewModel.SearchQuery = "Бо";
            
            viewModel.Suggestions.Should().HaveCount(2);
            viewModel.Suggestions.Should().Contain("Борщ");
        }
    }
}
