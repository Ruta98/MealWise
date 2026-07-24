using System;
using System.Collections.Generic;
using System.Text;

namespace MealWise.Models;

public class Recipe
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int EstimatedTimeMinutes { get; set; }

    // List of ingredients the AI decided to use from the Pantry
    public List<string> IngredientsUsed { get; set; } = new List<string>();

    // Step-by-step cooking instructions
    public List<string> Instructions { get; set; } = new List<string>();

    // Total nutritional value for the whole generated recipe
    public NutritionalValue Nutrition { get; set; } = new NutritionalValue();
}
