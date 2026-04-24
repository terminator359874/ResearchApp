using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RecipeSearchApp.Models;
using RecipeSearchApp.Services;

namespace RecipeSearchApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IDatabaseService _database;

    private string _searchQuery = string.Empty;
    private string _selectedAlgorithm = "KMP";
    private Recipe? _selectedRecipe;
    private string _highlightedInstructions = string.Empty;
    private string _cacheStats = string.Empty;
    private string _selectedFtsQueryType = "Простой поиск";

    public IDatabaseService Database => _database;

    public ObservableCollection<Recipe> Recipes { get; } = new();
    public ObservableCollection<string> Suggestions { get; } = new();

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery != value)
            {
                _searchQuery = value;
                OnPropertyChanged();
                UpdateSuggestions();
            }
        }
    }

    public string SelectedAlgorithm
    {
        get => _selectedAlgorithm;
        set
        {
            if (_selectedAlgorithm != value)
            {
                _selectedAlgorithm = value;
                OnPropertyChanged();
                UpdateHighlight();
            }
        }
    }

    public Recipe? SelectedRecipe
    {
        get => _selectedRecipe;
        set
        {
            if (_selectedRecipe != value)
            {
                _selectedRecipe = value;
                OnPropertyChanged();

                UpdateHighlight();

                if (value != null)
                {
                    _ = LoadRecipeDetailsAsync(value.Id);
                }
            }
        }
    }

    public string HighlightedInstructions
    {
        get => _highlightedInstructions;
        set
        {
            if (_highlightedInstructions != value)
            {
                _highlightedInstructions = value;
                OnPropertyChanged();
            }
        }
    }

    public string CacheStats
    {
        get => _cacheStats;
        set
        {
            if (_cacheStats != value)
            {
                _cacheStats = value;
                OnPropertyChanged();
            }
        }
    }

    public string SelectedFtsQueryType
    {
        get => _selectedFtsQueryType;
        set
        {
            if (_selectedFtsQueryType != value)
            {
                _selectedFtsQueryType = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand SearchCommand { get; }
    public ICommand LoadAllCommand { get; }
    public ICommand RefreshCacheStatsCommand { get; }

    public MainViewModel() : this(new DatabaseService())
    {
    }

    public MainViewModel(IDatabaseService database)
    {
        _database = database;

        SearchCommand = new RelayCommand(async () => await PerformSearchAsync());
        LoadAllCommand = new RelayCommand(async () => await LoadAllRecipesAsync());
        RefreshCacheStatsCommand = new RelayCommand(() =>
        {
            RefreshCacheStats();
            return Task.CompletedTask;
        });

        _ = InitializeAsync(); // ← ВАЖНО
    }

    private async Task InitializeAsync()
    {
        await _database.InitializeAsync();

        await LoadAllRecipesAsync();
        RefreshCacheStats();
    }
    private async Task PerformSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            await LoadAllRecipesAsync();
            return;
        }

        var ftsQuery = GenerateFtsQuery(SearchQuery);
        var results = await _database.FullTextSearchAsync(ftsQuery);

        Recipes.Clear();
        foreach (var recipe in results)
        {
            Recipes.Add(recipe);
        }
    }

    private string GenerateFtsQuery(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        
        var words = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        
        switch (SelectedFtsQueryType)
        {
            case "Поиск фразы":
                return $"\"{input}\"";
            case "Логическое И":
                return string.Join(" AND ", words);
            case "Логическое ИЛИ":
                return string.Join(" OR ", words);
            case "Исключение":
                if (words.Length > 1) return $"{words[0]} NOT {string.Join(" NOT ", words.Skip(1))}";
                return words[0];
            case "Префиксный поиск":
                return string.Join(" ", words.Select(w => $"{w}*"));
            case "Близость слов (NEAR)":
                return $"NEAR({string.Join(" ", words)}, 5)";
            case "Простой поиск":
            default:
                return input;
        }
    }

    private async Task LoadAllRecipesAsync()
    {
        var all = await _database.GetAllRecipesAsync();

        Recipes.Clear();
        foreach (var recipe in all)
        {
            Recipes.Add(recipe);
        }
    }

    private void UpdateSuggestions()
    {
        Suggestions.Clear();

        if (string.IsNullOrWhiteSpace(SearchQuery) || SearchQuery.Length < 2)
            return;

        var suggestions = _database.GetTitleSuggestions(SearchQuery);

        foreach (var suggestion in suggestions.Take(5))
        {
            Suggestions.Add(suggestion);
        }
    }

    private async Task LoadRecipeDetailsAsync(int id)
    {
        var recipe = await _database.GetRecipeByIdAsync(id);

        if (recipe != null)
        {
            var existing = Recipes.FirstOrDefault(r => r.Id == id);
            if (existing != null)
            {
                existing.ViewCount = recipe.ViewCount;
            }
        }

        RefreshCacheStats();
    }

    private void UpdateHighlight()
    {
        if (SelectedRecipe == null || string.IsNullOrWhiteSpace(SearchQuery))
        {
            HighlightedInstructions = SelectedRecipe?.Instructions ?? string.Empty;
            return;
        }

        HighlightedInstructions = StringSearcher.HighlightMatches(
            SelectedRecipe.Instructions,
            SearchQuery,
            SelectedAlgorithm
        );
    }

    private void RefreshCacheStats()
    {
        var stats = _database.GetCacheStatistics();

        CacheStats = $"Кеш: {stats["CachedItems"]} записей, {stats["TotalHits"]} обращений";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RelayCommand : ICommand
{
    private readonly Func<Task> _executeAsync;

    public RelayCommand(Func<Task> executeAsync)
    {
        _executeAsync = executeAsync;
    }

    public bool CanExecute(object? parameter) => true;

    public async void Execute(object? parameter)
    {
        await _executeAsync();
    }

    public event EventHandler? CanExecuteChanged;
}
