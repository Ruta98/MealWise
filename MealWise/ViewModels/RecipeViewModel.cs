using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MealWise.Models;
using MealWise.Services;

namespace MealWise.ViewModels;

public class RecipeViewModel : INotifyPropertyChanged
{
    private readonly AiService _aiService;
    private readonly DatabaseService _dbService;

    private string _summaryText = "Generated from 8 ingredients you have";
    public string SummaryText
    {
        get => _summaryText;
        set { _summaryText = value; OnPropertyChanged(); }
    }

    public ObservableCollection<RecipeSuggestionItem> SuggestedRecipes { get; set; } = new();
    public ObservableCollection<MissingIngredientItem> MissingIngredients { get; set; } = new();

    public ICommand BackCommand { get; }
    public ICommand SelectRecipeCommand { get; }
    public ICommand ToggleMissingItemCommand { get; }
    public ICommand AddMissingToShoppingListCommand { get; }

    public RecipeViewModel(AiService aiService, DatabaseService dbService)
    {
        _aiService = aiService;
        _dbService = dbService;

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        SelectRecipeCommand = new Command<RecipeSuggestionItem>(async (recipe) => await OnSelectRecipeAsync(recipe));
        ToggleMissingItemCommand = new Command<MissingIngredientItem>(OnToggleMissingItem);
        AddMissingToShoppingListCommand = new Command(async () => await OnAddMissingToShoppingListAsync());

        LoadMockOrAiData();
    }

    private void LoadMockOrAiData()
    {
        // Наповнення даними згідно з макетом
        SuggestedRecipes.Add(new RecipeSuggestionItem
        {
            Title = "Carrot and egg stir fry",
            CookingTimeMinutes = 20,
            IngredientsCount = 4,
            MatchPercentage = 96,
            Icon = "🍜"
        });

        SuggestedRecipes.Add(new RecipeSuggestionItem
        {
            Title = "Milk and egg toast bake",
            CookingTimeMinutes = 15,
            IngredientsCount = 3,
            MatchPercentage = 88,
            Icon = "🍞"
        });

        SuggestedRecipes.Add(new RecipeSuggestionItem
        {
            Title = "Carrot ginger soup",
            CookingTimeMinutes = 30,
            IngredientsCount = 5,
            MatchPercentage = 74,
            Icon = "🥣"
        });

        MissingIngredients.Add(new MissingIngredientItem { Name = "Ginger root", IsSelected = false });
        MissingIngredients.Add(new MissingIngredientItem { Name = "Bread loaf", IsSelected = false });
    }

    private async Task OnSelectRecipeAsync(RecipeSuggestionItem? recipe)
    {
        if (recipe == null) return;
        await Shell.Current.DisplayAlert("Recipe Selected", $"Opening details for {recipe.Title}...", "OK");
    }

    private void OnToggleMissingItem(MissingIngredientItem? item)
    {
        if (item != null)
        {
            item.IsSelected = !item.IsSelected;
        }
    }

    private async Task OnAddMissingToShoppingListAsync()
    {
        var selected = MissingIngredients.Where(i => i.IsSelected).ToList();
        if (!selected.Any())
        {
            // Якщо нічого не вибрано вручну — додаємо всі
            selected = MissingIngredients.ToList();
        }

        string names = string.Join(", ", selected.Select(s => s.Name));
        await Shell.Current.DisplayAlert("Shopping List", $"Added to list: {names}", "OK");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RecipeSuggestionItem
{
    public string Title { get; set; } = string.Empty;
    public int CookingTimeMinutes { get; set; }
    public int IngredientsCount { get; set; }
    public int MatchPercentage { get; set; }
    public string Icon { get; set; } = "🍲";
}

public class MissingIngredientItem : INotifyPropertyChanged
{
    public string Name { get; set; } = string.Empty;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}