using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using MealWise.Models;

namespace MealWise.Views;

[QueryProperty(nameof(SelectedRecipe), "Recipe")]
public partial class RecipeDetailsPage : ContentPage
{
    private Recipe? _selectedRecipe;
    public Recipe? SelectedRecipe
    {
        get => _selectedRecipe;
        set
        {
            _selectedRecipe = value;
            if (_selectedRecipe != null)
            {
                PopulateRecipeDetails(_selectedRecipe);
            }
        }
    }

    public RecipeDetailsPage()
    {
        InitializeComponent();
    }

    private void PopulateRecipeDetails(Recipe recipe)
    {
        RecipeTitleLabel.Text = recipe.Title;
        PrepTimeLabel.Text = $"{recipe.EstimatedTimeMinutes} minutes";
        DescriptionLabel.Text = recipe.Description;

        // Наповнюємо КБЖВ
        CaloriesLabel.Text = $"{recipe.Nutrition.Calories:F0}";
        ProteinLabel.Text = $"{recipe.Nutrition.ProteinGrams:F1}g";
        CarbsLabel.Text = $"{recipe.Nutrition.CarbsGrams:F1}g";
        FatLabel.Text = $"{recipe.Nutrition.FatGrams:F1}g";

        // Очищаємо контейнери
        IngredientsContainer.Children.Clear();
        InstructionsContainer.Children.Clear();

        // Динамічно додаємо інгредієнти
        foreach (var ingredient in recipe.IngredientsUsed)
        {
            var label = new Label
            {
                Text = $"•  {ingredient}",
                FontSize = 13.5,
                TextColor = Color.FromArgb("#F3F7F3"),
                LineHeight = 1.2
            };
            IngredientsContainer.Children.Add(label);
        }

        // Динамічно додаємо кроки приготування
        int stepNumber = 1;
        foreach (var step in recipe.Instructions)
        {
            var stepLayout = new VerticalStackLayout { Spacing = 2 };

            var stepHeader = new Label
            {
                Text = $"Step {stepNumber}",
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#C8FF4D")
            };

            var stepText = new Label
            {
                Text = step,
                FontSize = 13.5,
                TextColor = Color.FromArgb("#8C9F97"),
                LineHeight = 1.3
            };

            stepLayout.Children.Add(stepHeader);
            stepLayout.Children.Add(stepText);
            InstructionsContainer.Children.Add(stepLayout);

            stepNumber++;
        }
    }

    private async void OnBackTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}