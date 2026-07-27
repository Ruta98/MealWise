using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using MealWise.Models;
using MealWise.Services;
using Microsoft.Maui.Controls;

namespace MealWise.ViewModels;

public class RecipeViewModel : INotifyPropertyChanged
{
    private readonly AiService _aiService;
    private readonly DatabaseService _dbService;
    private readonly ProfileService _profileService;
    private readonly NutritionCalculator _nutritionCalculator;

    private string _summaryText = "Scanning your kitchen stock...";
    public string SummaryText
    {
        get => _summaryText;
        set { _summaryText = value; OnPropertyChanged(); }
    }

    private bool _isGenerating;
    public bool IsGenerating
    {
        get => _isGenerating;
        set { _isGenerating = value; OnPropertyChanged(); }
    }

    public ObservableCollection<RecipeSuggestionItem> SuggestedRecipes { get; set; } = new();
    public ObservableCollection<MissingIngredientItem> MissingIngredients { get; set; } = new();

    public ICommand BackCommand { get; }
    public ICommand SelectRecipeCommand { get; }
    public ICommand ToggleMissingItemCommand { get; }
    public ICommand AddMissingToShoppingListCommand { get; }

    public RecipeViewModel(
        AiService aiService,
        DatabaseService dbService,
        ProfileService profileService,
        NutritionCalculator nutritionCalculator)
    {
        _aiService = aiService;
        _dbService = dbService;
        _profileService = profileService;
        _nutritionCalculator = nutritionCalculator;

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        SelectRecipeCommand = new Command<RecipeSuggestionItem>(async (recipe) => await OnSelectRecipeAsync(recipe));
        ToggleMissingItemCommand = new Command<MissingIngredientItem>(OnToggleMissingItem);
        AddMissingToShoppingListCommand = new Command(async () => await OnAddMissingToShoppingListAsync());

        // Запуск генерації персоналізованого рецепта ШІ
        _ = GeneratePersonalizedRecipesAsync();
    }

    private async Task GeneratePersonalizedRecipesAsync()
    {
        IsGenerating = true;
        SuggestedRecipes.Clear();
        MissingIngredients.Clear();

        try
        {
            var profile = _profileService.GetProfile();
            var pantryItems = await _dbService.GetPantryItemsAsync();
            var todayMeals = await _dbService.GetDailyMealsAsync(DateTime.Today);

            var dailyTarget = profile.DailyTargetNutrition;

            // Розрахунок залишку КБЖВ на сьогодні
            var remainingNutrition = _nutritionCalculator.CalculateRemainingDailyBudget(dailyTarget, todayMeals);

            // Отримання 7-денної аналітики споживання для точніших рекомендацій ШІ (наприклад, фокус на клітковині)
            var startDate = DateTime.Today.AddDays(-7);
            var endDate = DateTime.Today;
            var pastMeals = await _dbService.GetMealEntriesForPeriodAsync(startDate, endDate);
            var weeklySummary = _nutritionCalculator.CalculatePeriodSummary(pastMeals, dailyTarget, 7);

            int availableCount = pantryItems.Count(i => i.QuantityAmount > 0);
            SummaryText = $"Scanning {availableCount} ingredients in your kitchen...";

            // Виклик інтегрованого сервісу ШІ (DeepSeek API)
            var generatedRecipe = await _aiService.GenerateRecipeAsync(
                profile,
                pantryItems,
                remainingNutrition,
                weeklySummary);

            if (generatedRecipe != null)
            {
                // Відображаємо згенеровану страву в списку рекомендованих
                SuggestedRecipes.Add(new RecipeSuggestionItem
                {
                    Title = generatedRecipe.Title,
                    CookingTimeMinutes = generatedRecipe.EstimatedTimeMinutes,
                    IngredientsCount = generatedRecipe.IngredientsUsed.Count,
                    MatchPercentage = CalculateMatchPercentage(generatedRecipe, pantryItems),
                    Icon = GetRecipeIcon(generatedRecipe),
                    ActualRecipe = generatedRecipe
                });

                SummaryText = $"Custom recipe generated from your stock!";

                // Порівнюємо інгредієнти рецепта з коморою, щоб виявити відсутні продукти
                var pantryNames = pantryItems.Select(p => p.Name.ToLower().Trim()).ToList();
                foreach (var ingredientNeeded in generatedRecipe.IngredientsUsed)
                {
                    bool isAvailable = pantryNames.Any(pName => ingredientNeeded.ToLower().Contains(pName));
                    if (!isAvailable)
                    {
                        MissingIngredients.Add(new MissingIngredientItem
                        {
                            Name = ingredientNeeded,
                            IsSelected = false
                        });
                    }
                }
            }
            else
            {
                SummaryText = "No recipes could be generated. Add more staples.";
            }
        }
        catch (Exception ex)
        {
            SummaryText = "Failed to load personalized recommendations.";
            System.Diagnostics.Debug.WriteLine($"Error during recipe generation: {ex.Message}");
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private int CalculateMatchPercentage(Recipe recipe, List<PantryItem> pantry)
    {
        if (recipe.IngredientsUsed.Count == 0) return 100;

        int matched = 0;
        var pantryNames = pantry.Select(p => p.Name.ToLower().Trim()).ToList();

        foreach (var ingredient in recipe.IngredientsUsed)
        {
            if (pantryNames.Any(pName => ingredient.ToLower().Contains(pName)))
            {
                matched++;
            }
        }

        double ratio = (double)matched / recipe.IngredientsUsed.Count;
        return (int)Math.Clamp(ratio * 100, 50, 100);
    }

    private string GetRecipeIcon(Recipe recipe)
    {
        string title = recipe.Title.ToLower();
        if (title.Contains("soup")) return "🥣";
        if (title.Contains("salad")) return "🥗";
        if (title.Contains("chicken") || title.Contains("meat")) return "🍗";
        if (title.Contains("fish") || title.Contains("salmon") || title.Contains("seafood")) return "🐟";
        if (title.Contains("egg") || title.Contains("scramble")) return "🍳";
        if (title.Contains("bread") || title.Contains("toast") || title.Contains("sandwich")) return "🍞";
        if (title.Contains("pasta") || title.Contains("noodle")) return "🍝";
        return "🍛";
    }

    private async Task OnSelectRecipeAsync(RecipeSuggestionItem? item)
    {
        if (item == null || item.ActualRecipe == null) return;

        // Створюємо словник параметрів для передачі об'єкта страви
        var navigationParameters = new Dictionary<string, object>
    {
        { "Recipe", item.ActualRecipe }
    };

        // Навігація до детальної сторінки
        await Shell.Current.GoToAsync("RecipeDetailsPage", navigationParameters);
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
            // Якщо нічого не вибрано вручную — додаємо все
            selected = MissingIngredients.ToList();
        }

        if (!selected.Any())
        {
            await Shell.Current.DisplayAlert("Shopping List", "No missing items to add.", "OK");
            return;
        }

        string names = string.Join(", ", selected.Select(s => s.Name));
        await Shell.Current.DisplayAlert("Shopping List", $"Added to shopping list: {names}", "OK");
    }

    #region PropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}

public class RecipeSuggestionItem
{
    public string Title { get; set; } = string.Empty;
    public int CookingTimeMinutes { get; set; }
    public int IngredientsCount { get; set; }
    public int MatchPercentage { get; set; }
    public string Icon { get; set; } = "🍲";
    public Recipe? ActualRecipe { get; set; }
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